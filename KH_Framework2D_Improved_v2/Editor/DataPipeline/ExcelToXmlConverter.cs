// Editor/DataPipeline/ExcelToXmlConverter.cs
// 
// Excel -> XML 변환기
// 
// 설치 필요:
// 1. NuGet에서 ExcelDataReader 패키지 추가
//    (Unity에서는 .dll을 직접 Plugins 폴더에 추가하거나 OpenUPM 사용)
// 
// 사용법:
// 1. Assets/Data/Excel/ 폴더에 .xlsx 파일 배치
// 2. Unity 메뉴: Tools > Data Pipeline > Convert All Excel to XML
// 3. Assets/Resources/Data/ 에 XML 파일 생성됨

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// ExcelDataReader가 설치되어 있지 않으면 주석 처리
// using ExcelDataReader;

namespace KH.Framework2D.Editor.Pipeline
{
    /// <summary>
    /// Excel 파일을 XML로 변환하는 유틸리티.
    /// 기획자가 Excel로 작업 -> 개발자가 버튼 한 번으로 XML 변환.
    /// </summary>
    public static class ExcelToXmlConverter
    {
        private const string ExcelFolder = "Assets/Data/Excel";
        private const string XmlFolder = "Assets/Resources/Data";
        
        [MenuItem("Tools/Data Pipeline/Convert All Excel to XML")]
        public static void ConvertAll()
        {
            // 폴더가 없으면 생성
            if (!Directory.Exists(ExcelFolder))
            {
                Directory.CreateDirectory(ExcelFolder);
                Debug.LogWarning($"[DataPipeline] Created folder: {ExcelFolder}");
                Debug.LogWarning("[DataPipeline] Place your .xlsx files in this folder and run again.");
                return;
            }
            
            if (!Directory.Exists(XmlFolder))
            {
                Directory.CreateDirectory(XmlFolder);
            }
            
            var excelFiles = Directory.GetFiles(ExcelFolder, "*.xlsx");
            
            if (excelFiles.Length == 0)
            {
                Debug.LogWarning($"[DataPipeline] No .xlsx files found in {ExcelFolder}");
                return;
            }
            
            int successCount = 0;
            foreach (var excelPath in excelFiles)
            {
                try
                {
                    ConvertFile(excelPath);
                    successCount++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DataPipeline] Failed to convert {Path.GetFileName(excelPath)}: {ex.Message}");
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"[DataPipeline] Converted {successCount}/{excelFiles.Length} files.");
        }
        
        [MenuItem("Tools/Data Pipeline/Convert Selected Excel")]
        public static void ConvertSelected()
        {
            var selected = Selection.activeObject;
            if (selected == null)
            {
                Debug.LogWarning("[DataPipeline] No file selected.");
                return;
            }
            
            var path = AssetDatabase.GetAssetPath(selected);
            if (!path.EndsWith(".xlsx"))
            {
                Debug.LogWarning("[DataPipeline] Selected file is not an Excel file (.xlsx)");
                return;
            }
            
            try
            {
                ConvertFile(path);
                AssetDatabase.Refresh();
                Debug.Log($"[DataPipeline] Converted: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataPipeline] Failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 단일 Excel 파일을 XML로 변환.
        /// 각 시트는 별도의 XML 파일로 저장됨.
        /// </summary>
        public static void ConvertFile(string excelPath)
        {
            // ExcelDataReader 사용 시:
            // using var stream = File.Open(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            // using var reader = ExcelReaderFactory.CreateReader(stream);
            // var result = reader.AsDataSet(new ExcelDataSetConfiguration
            // {
            //     ConfigureDataTable = _ => new ExcelDataTableConfiguration
            //     {
            //         UseHeaderRow = true
            //     }
            // });
            
            // 임시: CSV 기반 변환 (ExcelDataReader 없이 테스트용)
            // 실제 사용 시 ExcelDataReader로 교체
            var fileName = Path.GetFileNameWithoutExtension(excelPath);
            var csvPath = excelPath.Replace(".xlsx", ".csv");
            
            if (File.Exists(csvPath))
            {
                ConvertCsvToXml(csvPath, Path.Combine(XmlFolder, $"{fileName}.xml"), fileName);
            }
            else
            {
                Debug.LogWarning($"[DataPipeline] ExcelDataReader not installed. Please export {fileName}.xlsx as CSV.");
                CreateSampleXml(fileName);
            }
        }
        
        /// <summary>
        /// CSV를 XML로 변환 (ExcelDataReader 없을 때 대안)
        /// </summary>
        private static void ConvertCsvToXml(string csvPath, string xmlPath, string rootName)
        {
            var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            if (lines.Length < 2) return; // 헤더 + 최소 1행 필요
            
            var headers = ParseCsvLine(lines[0]);
            var sb = new StringBuilder();
            
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<Rows>");
            
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                
                var values = ParseCsvLine(lines[i]);
                sb.AppendLine("  <Row>");
                
                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    var escaped = EscapeXml(values[j]);
                    sb.AppendLine($"    <{headers[j]}>{escaped}</{headers[j]}>");
                }
                
                sb.AppendLine("  </Row>");
            }
            
            sb.AppendLine("</Rows>");
            
            Directory.CreateDirectory(Path.GetDirectoryName(xmlPath));
            File.WriteAllText(xmlPath, sb.ToString(), Encoding.UTF8);
            
            Debug.Log($"[DataPipeline] Created: {xmlPath}");
        }
        
        /// <summary>
        /// CSV 라인 파싱 (쉼표 분리, 따옴표 처리)
        /// </summary>
        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++; // Skip escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            
            result.Add(sb.ToString().Trim());
            return result.ToArray();
        }
        
        /// <summary>
        /// XML 특수문자 이스케이프
        /// </summary>
        private static string EscapeXml(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
        
        /// <summary>
        /// 샘플 XML 생성 (테스트용)
        /// </summary>
        private static void CreateSampleXml(string name)
        {
            var xmlPath = Path.Combine(XmlFolder, $"{name}.xml");
            
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<Rows>");
            sb.AppendLine("  <!-- Sample data - replace with actual Excel export -->");
            sb.AppendLine("  <Row>");
            sb.AppendLine("    <Id>sample_001</Id>");
            sb.AppendLine("    <Name>Sample Item</Name>");
            sb.AppendLine("    <Value>100</Value>");
            sb.AppendLine("  </Row>");
            sb.AppendLine("</Rows>");
            
            Directory.CreateDirectory(Path.GetDirectoryName(xmlPath));
            File.WriteAllText(xmlPath, sb.ToString(), Encoding.UTF8);
            
            Debug.Log($"[DataPipeline] Created sample XML: {xmlPath}");
        }
        
        [MenuItem("Tools/Data Pipeline/Create Sample Excel Template")]
        public static void CreateSampleTemplate()
        {
            // Excel 템플릿 정보 출력 (실제 .xlsx 생성은 복잡하므로 가이드만 제공)
            var info = @"
=== Excel 템플릿 가이드 ===

1. Cards.xlsx
   | Id          | Name       | Cost | Type   | Effect              | SkillId    |
   |-------------|------------|------|--------|---------------------|------------|
   | card_001    | Strike     | 1    | Attack | Deal 6 damage       | skill_001  |
   | card_002    | Defend     | 1    | Skill  | Gain 5 block        |            |

2. Units.xlsx
   | Id          | Name       | HP   | ATK  | PrefabPath          |
   |-------------|------------|------|------|---------------------|
   | unit_001    | Goblin     | 30   | 5    | Prefabs/Units/Goblin|

3. Skills.xlsx
   | Id          | Name       | Damage | TargetType |
   |-------------|------------|--------|------------|
   | skill_001   | Slash      | 6      | Single     |

규칙:
- 첫 행은 반드시 컬럼명 (영문, 공백 없음)
- Id는 고유해야 함
- 빈 셀은 빈 문자열로 처리됨
- 시트 이름 = XML 파일 이름
";
            Debug.Log(info);
            
            // CSV 템플릿 생성
            var csvPath = Path.Combine(ExcelFolder, "Cards_Template.csv");
            if (!Directory.Exists(ExcelFolder))
                Directory.CreateDirectory(ExcelFolder);
                
            var csv = "Id,Name,Cost,Type,Effect,SkillId\ncard_001,Strike,1,Attack,Deal 6 damage,skill_001\ncard_002,Defend,1,Skill,Gain 5 block,";
            File.WriteAllText(csvPath, csv);
            
            AssetDatabase.Refresh();
            Debug.Log($"[DataPipeline] Created CSV template: {csvPath}");
        }
    }
}

#endif
