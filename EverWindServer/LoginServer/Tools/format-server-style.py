"""서버 C++ 선언 순서와 사용자 지정 빈 줄 규칙을 정리한다."""
from pathlib import Path
import re
import subprocess
import sys
from collections import Counter

ROOT = Path(__file__).resolve().parents[1]
FORMATTER = Path('C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/Llvm/bin/clang-format.exe')
TOKEN = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_][A-Za-z_0-9]*|::|->|&&|\|\||<<|>>|[^\s]', re.M)

def tokens(source):
    return [(m.group(), m.start(), m.end()) for m in TOKEN.finditer(source) if not m.group().startswith(('//', '/*'))]

def declaration_kind(text):
    clean = re.sub(r'//[^\n]*|/\*[\s\S]*?\*/', '', text).strip()
    if re.search(r'\(\s*\*\s*\w+\s*\)', clean): return 'variable'
    if re.search(r'\boperator\b', clean): return 'function'
    if re.match(r'(using|typedef|enum|struct|class)\b', clean):
        return 'type'
    # 함수 형식을 가진 템플릿 인자는 멤버 함수 선언으로 취급하지 않는다.
    angle = 0
    for value, _, _ in tokens(clean):
        if value == '<': angle += 1
        elif value == '>': angle = max(0, angle - 1)
        elif value == '>>': angle = max(0, angle - 2)
        elif value == '(' and angle == 0: return 'function'
        elif value in ['=', '{', ';'] and angle == 0: return 'variable'
    return 'variable'

def reorder_records(source):
    ts = tokens(source)
    edits = []
    for i, (value, start, _) in enumerate(ts):
        if value not in ['class', 'struct'] or (i and ts[i-1][0] == 'enum'):
            continue
        j = i + 1
        while j < len(ts) and ts[j][0] not in ['{', ';']: j += 1
        if j == len(ts) or ts[j][0] != '{': continue
        depth = 1; end = j + 1
        while depth:
            depth += (ts[end][0] == '{') - (ts[end][0] == '}'); end += 1
        close = end - 1
        # 이 파일의 레코드는 서로 중첩되지 않는다.
        if any(a <= start < b for a, b, _ in edits): continue
        access = 'private' if value == 'class' else 'public'
        groups = {'private': [], 'protected': [], 'public': []}
        k = j + 1; chunk_start = ts[j][2]
        while k < close:
            if ts[k][0] in groups and k + 1 < close and ts[k+1][0] == ':':
                access = ts[k][0]; k += 2; chunk_start = ts[k-1][2]; continue
            begin = k; braces = parens = 0; function_body = False
            while k < close:
                v = ts[k][0]
                if v == '{' and braces == 0:
                    prefix = source[ts[begin][1]:ts[k][1]]
                    function_body = declaration_kind(prefix) == 'function'
                braces += (v == '{') - (v == '}')
                parens += (v == '(') - (v == ')')
                if v == ';' and braces == 0 and parens == 0: break
                if v == '}' and braces == 0 and parens == 0 and function_body:
                    if k + 1 < close and ts[k+1][0] == ';': k += 1
                    break
                k += 1
            if k >= close: break
            text = source[chunk_start:ts[k][2]].strip()
            if text: groups[access].append((declaration_kind(text), text))
            chunk_start = ts[k][2]; k += 1
        if not any(groups.values()): continue
        has_explicit = any(t[0] in ['private', 'public', 'protected'] for t in ts[j+1:close])
        # 접근 권한은 바꾸지 않고 각 영역 안에서 자료형 선언, 변수, 함수를 배치한다.
        order = ['private', 'protected', 'public'] if value == 'class' or has_explicit else ['public']
        blocks = []
        for label in order:
            members = groups[label]
            if not members: continue
            members = [x for kind in ['type','variable','function'] for x in members if x[0] == kind]
            blocks.append((label + ':\n' if has_explicit or value == 'class' else '') + '\n'.join(text for _, text in members))
        edits.append((ts[j][2], ts[close][1], '\n' + '\n\n'.join(blocks) + '\n'))
    for a, b, replacement in sorted(edits, reverse=True): source = source[:a] + replacement + source[b:]
    return source

def flatten_parameters(source):
    ts = tokens(source); stack = []; edits = []
    for value, start, end in ts:
        if value == '(': stack.append(start)
        elif value == ')' and stack:
            begin = stack.pop()
            if stack: continue
            text = source[begin:end]
            # 람다 본문은 자체 중괄호와 문장 줄바꿈을 유지한다.
            is_lambda=bool(re.search(r'\]\s*(?:\([^)]*\))?\s*(?:mutable\s*)?(?:noexcept\s*)?\{',text))
            if '\n' in text and is_lambda:
                braces=[(v,a,b) for v,a,b in tokens(text) if v in ['{','}']]
                if braces:
                    first=braces[0][1];last=braces[-1][2]
                    prefix=re.sub(r'\s*\n\s*',' ',text[:first]).rstrip()
                    prefix=re.sub(r'^\(\s+','(',prefix)
                    caller_line=source.rfind('\n',0,begin)+1
                    indent=re.match(r'[ \t]*',source[caller_line:]).group()
                    brace_line=text.rfind('\n',0,first)+1
                    old_indent=first-brace_line
                    body=text[first:last].splitlines()
                    body=[body[0]]+[line[old_indent:] if len(line)-len(line.lstrip())>=old_indent else line.lstrip() for line in body[1:]]
                    joined=prefix+'\n'+indent+('\n'+indent).join(body)+text[last:]
                    edits.append((begin,end,joined))
            elif '\n' in text and '//' not in text and '/*' not in text:
                joined=re.sub(r'\s*\n\s*',' ',text)
                joined=re.sub(r'^\(\s+', '(', joined)
                joined=re.sub(r'\s+\)$', ')', joined)
                edits.append((begin,end,joined))
    for a,b,text in reversed(edits): source=source[:a]+text+source[b:]
    return source

def variable_type(line):
    s = re.sub(r'//.*', '', line).strip()
    if not s or s.startswith(('return ', 'throw ', 'delete ', 'break', 'continue', 'using ', '#', 'case ', 'else', 'if ', 'for ', 'while ', 'switch ')):
        return None
    if not s.endswith(';') or re.search(r'\boperator\b',s): return None
    # 객체의 직접 생성 구문도 같은 타입의 연속 선언으로 묶는다.
    direct=re.match(r'((?:(?:const|static|constexpr|inline|volatile|mutable|unsigned|signed|long|short)\s+)*[\w:]+(?:<.*>)?)\s+([*&\s]*)([A-Za-z_]\w*)\s*\((.*)\);$',s)
    if direct and re.search(r'[.\"\']|\b\w+\s*\(',direct.group(4)):
        return re.sub(r'\s+',' ',direct.group(1)).strip()+direct.group(2).replace(' ','')
    if '(' in s.split('=')[0]: return None
    m = re.match(r'((?:(?:const|static|constexpr|inline|volatile|mutable|unsigned|signed|long|short)\s+)*[\w:]+(?:<.*>)?)\s+([*&\s]*)([A-Za-z_]\w*)\s*(?:[=;{\[,])',s)
    return re.sub(r'\s+',' ',m.group(1)).strip()+m.group(2).replace(' ','') if m else None


def callee(line):
    s=line.strip()
    if not s.endswith(';') or s.startswith(('return ','throw ','if ','for ','while ')): return None
    names=re.findall(r'(?:\.|->|::|\b)([A-Za-z_]\w*)\s*(?:<[^;]*>)?\s*\(',s)
    return names[0] if names else None

def space_statements(source):
    # 여러 줄 람다 인수의 닫는 줄도 원래 호출 이름으로 비교한다.
    original_lines=source.splitlines()
    start_names={}; end_names={}; stack=[]; ts=tokens(source)
    for index,(value,a,b) in enumerate(ts):
        if value=='(': stack.append(index)
        elif value==')' and stack:
            begin=stack.pop()
            if stack or not begin or index+1>=len(ts) or ts[index+1][0]!=';': continue
            name=ts[begin-1][0]
            if not re.fullmatch(r'[A-Za-z_]\w*',name) or name in ['if','for','while','switch']:continue
            start_line=source.count('\n',0,ts[begin][1]);end_line=source.count('\n',0,b)
            start_names[start_line]=name;end_names[end_line]=name
    lines=[(i,line.rstrip()) for i,line in enumerate(original_lines) if line.strip()]
    out=[]; parens=0; brace_bases=[]
    previous_index=-1
    for original_index,line in lines:
        prev=out[-1] if out else ''
        stripped=line.strip(); ps=prev.strip()
        indent=len(line)-len(line.lstrip()); pi=len(prev)-len(prev.lstrip())
        gap=False
        if prev and not stripped.startswith(('#','//')) and not ps.startswith('#'):
            if parens==(brace_bases[-1] if brace_bases else 0) and indent==pi:
                if ps.endswith(';') and not stripped.startswith(('}', 'else', 'catch', ':', '.')):
                    t1,t2=variable_type(prev),variable_type(line)
                    gap=(t1!=t2) if (t1 and t2) else not((end_names.get(previous_index) or callee(prev)) and (end_names.get(previous_index) or callee(prev))==(start_names.get(original_index) or callee(line)))
                if ps=='}' and not stripped.startswith(('}', ';', ')', ',', 'else', 'catch')): gap=True
            if stripped in ['private:','public:','protected:'] or ps in ['private:','public:','protected:']: gap=False
        if gap and out and out[-1]!='': out.append('')
        out.append(line)
        previous_index=original_index
        for v,_,_ in tokens(line):
            parens+=(v=='(')-(v==')')
            if v=='{':brace_bases.append(parens)
            elif v=='}' and brace_bases:brace_bases.pop()
    return '\n'.join(out)+'\n'

def fingerprint(source):
    return [v for v,_,_ in tokens(source) if v not in ['{','}']]

def run():
    files=sorted(p for p in ROOT.rglob('*') if p.suffix in ['.h','.cpp'] and not any(x in p.relative_to(ROOT).parts for x in ['bin','obj','output']))
    changed=0
    for p in files:
        original=p.read_text(encoding='utf-8-sig')
        source=reorder_records(original) if p.suffix=='.h' else original
        def declaration_tokens(value):
            return Counter(fingerprint(re.sub(r'\b(public|private|protected)\s*:', '', value)))
        if declaration_tokens(source)!=declaration_tokens(original):
            raise RuntimeError(str(p)+': 선언 재배치 중 코드 토큰이 변경되어 중단함')
        source=subprocess.run([str(FORMATTER),'--assume-filename='+str(p)],input=source,capture_output=True,text=True,encoding='utf-8',check=True).stdout
        source=flatten_parameters(source)
        source=re.sub(r'(>)[ \t]*\n[ \t]+(\w+;)',r'\1 \2',source)
        source=space_statements(source)
        if source!=original:
            p.write_text(source,encoding='utf-8')
            changed+=1
    print(f'서버 C++ 파일 {len(files)}개 확인, {changed}개 스타일 정리 완료')

if __name__=='__main__': run()
