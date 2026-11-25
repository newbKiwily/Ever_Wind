using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EverWind.EntityEditing
{
    // 지정한 콘텐츠 행만 편집하고 나머지 정의와 주석은 원문 그대로 보존한다.
    public sealed class EntityContentFile
    {
        readonly string original;
        readonly string newline;
        readonly string[] lines;
        readonly Dictionary<string, Dictionary<int, string[]>> rows = new Dictionary<string, Dictionary<int, string[]>>();
        readonly Dictionary<int, List<string[]>> ingredients = new Dictionary<int, List<string[]>>();
        static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public EntityContentFile(string text)
        {
            original = text;
            newline = text.Contains("\r\n") ? "\r\n" : "\n";
            lines = text.TrimStart('\uFEFF').Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool format = false;
            foreach (string line in lines)
            {
                string value = line.Trim();
                if (value.Length == 0 || value.StartsWith("#")) continue;
                string[] p = Regex.Matches(value, "\"[^\"]*\"|\\S+").Cast<Match>().Select(m => m.Value).ToArray();
                if (!format)
                {
                    if (value != "format 1") throw new InvalidOperationException("GameData.ewdata 첫 행은 format 1이어야 합니다.");
                    format = true;
                    continue;
                }
                if (p[0] != "monster" && p[0] != "resource" && p[0] != "item" && p[0] != "skill" && p[0] != "player" && p[0] != "recipe" && p[0] != "ingredient") continue;
                int expected = p[0] == "monster" ? 11 : p[0] == "resource" ? 7 : p[0] == "player" ? 3 : p[0] == "item" ? 9 : p[0] == "skill" ? 12 : 5;
                if (p.Length != expected) throw new InvalidOperationException("기존 콘텐츠 행의 필드 수가 잘못되었습니다: " + value);
                int id = p[0] == "player" ? 0 : int.Parse(p[1], Culture);
                if (p[0] == "ingredient")
                {
                    if (!Has("recipe", id)) throw new InvalidOperationException("재료 행보다 제작법 정의가 먼저 있어야 합니다: " + id);
                    if (!ingredients.TryGetValue(id, out var list)) ingredients.Add(id, list = new List<string[]>());
                    if (int.Parse(p[2], Culture) != list.Count) throw new InvalidOperationException("제작 재료 인덱스는 0부터 연속이어야 합니다: " + id);
                    list.Add(p);
                    continue;
                }
                if (!rows.TryGetValue(p[0], out var group)) rows.Add(p[0], group = new Dictionary<int, string[]>());
                if (group.ContainsKey(id)) throw new InvalidOperationException("기존 콘텐츠 ID 중복: " + p[0] + " " + id);
                group.Add(id, p);
            }
            if (!format || !Has("player", 0)) throw new InvalidOperationException("format/player 행이 없습니다.");
        }

        public static EntityContentFile Read() => new EntityContentFile(File.ReadAllText(MapEditing.MapEditorExport.ContentPath, Encoding.UTF8));
        public bool Has(string type, int id) => rows.TryGetValue(type, out var group) && group.ContainsKey(id);
        public IEnumerable<string[]> Rows(string type) => rows.TryGetValue(type, out var group) ? group.Values : Enumerable.Empty<string[]>();
        public int NextId(string type) => Rows(type).Select(p => int.Parse(p[1], Culture)).DefaultIfEmpty(0).Max() + 1;
        public int NextVisualId() => Rows("monster").Select(p => int.Parse(p[2], Culture)).DefaultIfEmpty(-1).Max() + 1;

        public string[] Row(string type, int id)
        {
            if (!Has(type, id)) throw new InvalidOperationException("콘텐츠 정의가 없습니다: " + type + " " + id);
            return rows[type][id];
        }

        public string Upsert(string type, int id, IEnumerable<string> replacement, bool replace)
        {
            if (type != "item" && type != "recipe" && type != "resource" && type != "skill") throw new InvalidOperationException("지원하지 않는 편집 행입니다.");
            if (Has(type, id) != replace) throw new InvalidOperationException(replace ? "수정할 서버 정의가 없습니다." : "이미 사용 중인 서버 정의 ID입니다.");
            string[] block = replacement.ToArray();
            if (block.Length == 0) throw new InvalidOperationException("저장할 콘텐츠 행이 없습니다.");
            string result;
            if (!replace) result = original + (original.EndsWith("\n") ? "" : newline) + string.Join(newline, block) + newline;
            else
            {
                var output = new List<string>();
                foreach (string line in lines)
                {
                    var p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    bool match = p.Length > 1 && int.TryParse(p[1], out int lineId) && lineId == id;
                    if (match && p[0] == type) output.AddRange(block);
                    else if (!(match && (type == "recipe" && p[0] == "ingredient" || type == "skill" && (p[0] == "skillhit" || p[0] == "skillro" || p[0] == "skillrank")))) output.Add(line);
                }
                result = string.Join(newline, output);
            }
            new EntityContentFile(result);
            return result;
        }

        public void Validate(EntityEditorData data)
        {
            if (data.Alive && (data.Attack < 0 || data.Defence < 0)) throw new InvalidOperationException("공격력·방어력은 0 이상이어야 합니다.");
            if (data.Kind == EntityEditorKind.Player) return;
            string type = data.Kind == EntityEditorKind.Monster ? "monster" : "resource";
            if (data.DefinitionId <= 0 || Has(type, data.DefinitionId)) throw new InvalidOperationException("사용하지 않는 양수 정의 ID를 입력하세요.");
            Positive(data.Radius, "반경");
            if (data.Kind == EntityEditorKind.Monster)
            {
                if (data.VisualId < 0 || data.VisualId > 65535 || Rows("monster").Any(p => int.Parse(p[2], Culture) == data.VisualId))
                    throw new InvalidOperationException("사용하지 않는 외형 ID(0~65535)를 입력하세요.");
                if (data.MaxHp <= 0 || data.Experience < 0) throw new InvalidOperationException("HP는 양수, 경험치는 0 이상이어야 합니다.");
                NonNegative(data.MoveSpeed, "이동 속도");
                NonNegative(data.AttackCooldown, "공격 주기");
                if (!Has("skill", data.AttackSkillId)) throw new InvalidOperationException("서버에 존재하는 공격 스킬 ID를 입력하세요.");
                string[] skill = Rows("skill").First(p => int.Parse(p[1], Culture) == data.AttackSkillId);
                int kind = int.Parse(skill[10], Culture);
                if (kind != 0 && kind != 1) throw new InvalidOperationException("현재 몬스터 AI는 단일·즉발 원거리 스킬만 지원합니다.");
            }
            else
            {
                if (!Has("item", data.ItemId) || data.Capacity <= 0) throw new InvalidOperationException("기존 지급 아이템 ID와 양수 채집량이 필요합니다.");
                NonNegative(data.GatherTime, "채집 시간");
                NonNegative(data.RefillDelay, "리필 지연");
            }
        }

        public string Compose(EntityEditorData data)
        {
            Validate(data);
            string row;
            if (data.Kind == EntityEditorKind.Player)
            {
                row = "player " + data.Attack + " " + data.Defence;
                return string.Join(newline, lines.Select(line => line.TrimStart().StartsWith("player ") || line.TrimStart().StartsWith("player\t") ? row : line));
            }
            if (data.Kind == EntityEditorKind.Monster)
                row = string.Join(" ", "monster", data.DefinitionId, data.VisualId, data.Attack, data.Defence, F(data.MoveSpeed), data.MaxHp, F(data.Radius), data.Experience, data.AttackSkillId, F(data.AttackCooldown));
            else
                row = string.Join(" ", "resource", data.DefinitionId, data.ItemId, data.Capacity, F(data.Radius), F(data.GatherTime), F(data.RefillDelay));
            return original + (original.EndsWith("\n") ? "" : newline) + row + newline;
        }

        public static void Positive(float value, string name)
        {
            if (!Finite(value) || value <= 0) throw new InvalidOperationException(name + "은 유한한 양수여야 합니다.");
        }
        public static void NonNegative(float value, string name)
        {
            if (!Finite(value) || value < 0) throw new InvalidOperationException(name + "은 유한한 0 이상 값이어야 합니다.");
        }
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static string F(float value) => value.ToString("R", Culture);
    }
}
