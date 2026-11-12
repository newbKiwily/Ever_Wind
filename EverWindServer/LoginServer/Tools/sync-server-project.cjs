process.chdir(require('path').resolve(__dirname, '../../..'));
const fs=require('fs'),path=require('path'),crypto=require('crypto');
const base='EverWindServer/LoginServer';
function files(dir,ext){return fs.readdirSync(dir,{withFileTypes:true}).flatMap(e=>e.isDirectory()?(['bin','obj'].includes(e.name)?[]:files(dir+'/'+e.name,ext)):e.name.endsWith(ext)?[dir+'/'+e.name]:[]).sort();}
function win(s){return s.replaceAll('/','\\');}
function xml(s){return s.replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('"','&quot;');}
function save(file,s){fs.writeFileSync(file,s.replace(/\r\n/g,'\n').replace(/\n/g,'\r\n'));}
function group(tag,entries,dir){return '  <ItemGroup>\n'+entries.map(f=>'    <'+tag+' Include="'+xml(win(path.relative(dir,f)))+'" />').join('\n')+'\n  </ItemGroup>';}
function guid(name){const h=crypto.createHash('md5').update(name).digest('hex').toUpperCase();return '{'+[h.slice(0,8),h.slice(8,12),h.slice(12,16),h.slice(16,20),h.slice(20)].join('-')+'}';}
function filters(project,sources,headers){
 const dir=path.dirname(project),entries=[];
 for(const [kind,tag,list] of [['Source Files','ClCompile',sources],['Header Files','ClInclude',headers]])for(const file of list){
   const logical=file.startsWith(base+'/')?path.dirname(path.relative(base,file)).replaceAll('\\','/'): 'Tests';
   entries.push({tag,file,filter:kind+(logical!=='.'?'\\'+win(logical):'')});
 }
 const groups=new Set();for(const e of entries){const parts=e.filter.split('\\');while(parts.length){groups.add(parts.join('\\'));parts.pop();}}
 save(project+'.filters','<?xml version="1.0" encoding="utf-8"?>\n<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">\n  <ItemGroup>\n'+[...groups].sort().map(f=>'    <Filter Include="'+xml(f)+'"><UniqueIdentifier>'+guid(project+f)+'</UniqueIdentifier></Filter>').join('\n')+'\n  </ItemGroup>\n  <ItemGroup>\n'+entries.map(e=>'    <'+e.tag+' Include="'+xml(win(path.relative(dir,e.file)))+'"><Filter>'+xml(e.filter)+'</Filter></'+e.tag+'>').join('\n')+'\n  </ItemGroup>\n</Project>\n');
}
const sources=files(base,'.cpp'),headers=files(base,'.h'),project=base+'/LoginServer.vcxproj';
let text=fs.readFileSync(project,'utf8');
text=text.replace(/  <ItemGroup>\s*<ClCompile Include=[\s\S]*?<\/ItemGroup>/,group('ClCompile',sources,base));
text=text.replace(/  <ItemGroup>\s*<ClInclude Include=[\s\S]*?<\/ItemGroup>/,group('ClInclude',headers,base));
save(project,text);filters(project,sources,headers);
