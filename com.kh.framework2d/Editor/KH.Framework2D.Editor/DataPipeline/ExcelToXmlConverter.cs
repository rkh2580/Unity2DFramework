using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEditor;

namespace KH.Framework2D.Editor.DataPipeline
{
    /// <summary>
    /// Excel/CSV to XML converter for data pipeline.
    /// 
    /// Workflow:
    /// 1. Export Excel sheet as CSV (UTF-8)
    /// 2. Place CSV in DataTables/ folder
    /// 3. Run converter via menu or auto-import
    /// 4. XML is generated in Resources/Data/
    /// 
    /// CSV Format:
    /// - First row: Column headers (become XML element names)
    /// - Subsequent rows: Data values
    /// - Comments: Rows starting with # are ignored
    /// - Skip columns: Columns starting with @ are ignored
    /// </summary>
    public class ExcelToXmlConverter : EditorWindow
    {
        private const string CSV_FOLDER = "Assets/DataTables";
        private const string XML_OUTPUT_FOLDER = "Assets/Resources/Data";
        
        private string _csvFolderPath = CSV_FOLDER;
        private string _xmlOutputPath = XML_OUTPUT_FOLDER;
        private bool _autoConvertOnImport = true;
        
        [MenuItem("Tools/Data Pipeline/Open Converter Window")]
        public static void OpenWindow()
        {
            var window = GetWindow<ExcelToXmlConverter>("Data Converter");
            window.minSize = new Vector2(400, 300);
        }
        
        [MenuItem("Tools/Data Pipeline/Convert All CSV to XML")]
        public static void ConvertAllCsvToXml()
        {
            ConvertAllCsvFiles(CSV_FOLDER, XML_OUTPUT_FOLDER);
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Data Pipeline Converter", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "Workflow:\n" +
                "1. Export Excel sheets as CSV (UTF-8)\n" +
                "2. Place CSV files in DataTables/ folder\n" +
                "3. Click 'Convert All' or enable auto-convert\n" +
                "4. XML files are generated in Resources/Data/",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            // Folder paths
            EditorGUILayout.LabelField("Paths", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _csvFolderPath = EditorGUILayout.TextField("CSV Folder", _csvFolderPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var path = EditorUtility.OpenFolderPanel("Select CSV Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    _csvFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            _xmlOutputPath = EditorGUILayout.TextField("XML Output", _xmlOutputPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var path = EditorUtility.OpenFolderPanel("Select XML Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    _xmlOutputPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Options
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            _autoConvertOnImport = EditorGUILayout.Toggle("Auto-convert on Import", _autoConvertOnImport);
            
            EditorGUILayout.Space(20);
            
            // Actions
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Convert All CSV to XML", GUILayout.Height(30)))
            {
                ConvertAllCsvFiles(_csvFolderPath, _xmlOutputPath);
            }
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Sample CSV"))
            {
                CreateSampleCsv();
            }
            if (GUILayout.Button("Open CSV Folder"))
            {
                EditorUtility.RevealInFinder(_csvFolderPath);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Convert all CSV files in folder to XML.
        /// </summary>
        public static void ConvertAllCsvFiles(string csvFolder, string xmlOutput)
        {
            if (!Directory.Exists(csvFolder))
            {
                Directory.CreateDirectory(csvFolder);
                Debug.Log($"[DataPipeline] Created CSV folder: {csvFolder}");
            }
            
            if (!Directory.Exists(xmlOutput))
            {
                Directory.CreateDirectory(xmlOutput);
                Debug.Log($"[DataPipeline] Created XML output folder: {xmlOutput}");
            }
            
            var csvFiles = Directory.GetFiles(csvFolder, "*.csv", SearchOption.AllDirectories);
            
            if (csvFiles.Length == 0)
            {
                Debug.LogWarning("[DataPipeline] No CSV files found in: " + csvFolder);
                return;
            }
            
            int converted = 0;
            
            foreach (var csvFile in csvFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(csvFile);
                    var xmlPath = Path.Combine(xmlOutput, fileName + ".xml");
                    
                    ConvertCsvToXml(csvFile, xmlPath);
                    converted++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DataPipeline] Failed to convert {csvFile}: {ex.Message}");
                }
            }
            
            AssetDatabase.Refresh();
            Debug.Log($"[DataPipeline] Converted {converted}/{csvFiles.Length} CSV files to XML");
        }
        
        /// <summary>
        /// Convert a single CSV file to XML.
        /// </summary>
        public static void ConvertCsvToXml(string csvPath, string xmlPath)
        {
            var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
            
            if (lines.Length < 2)
            {
                Debug.LogWarning($"[DataPipeline] CSV file has no data rows: {csvPath}");
                return;
            }
            
            // Parse header row
            var headers = ParseCsvLine(lines[0]);
            
            // Find columns to skip (starting with @)
            var skipColumns = new HashSet<int>();
            for (int i = 0; i < headers.Count; i++)
            {
                if (headers[i].StartsWith("@"))
                {
                    skipColumns.Add(i);
                }
            }
            
            // Build XML
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = Encoding.UTF8
            };
            
            using (var writer = XmlWriter.Create(xmlPath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("DataTable");
                
                // Write metadata
                writer.WriteAttributeString("source", Path.GetFileName(csvPath));
                writer.WriteAttributeString("generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                
                // Write data rows
                for (int rowIndex = 1; rowIndex < lines.Length; rowIndex++)
                {
                    var line = lines[rowIndex].Trim();
                    
                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        continue;
                    
                    var values = ParseCsvLine(line);
                    
                    writer.WriteStartElement("Row");
                    
                    for (int colIndex = 0; colIndex < headers.Count && colIndex < values.Count; colIndex++)
                    {
                        if (skipColumns.Contains(colIndex))
                            continue;
                        
                        var header = headers[colIndex].Trim();
                        var value = values[colIndex].Trim();
                        
                        // Skip empty headers
                        if (string.IsNullOrEmpty(header))
                            continue;
                        
                        // Sanitize header for XML element name
                        header = SanitizeXmlName(header);
                        
                        writer.WriteElementString(header, value);
                    }
                    
                    writer.WriteEndElement(); // Row
                }
                
                writer.WriteEndElement(); // DataTable
                writer.WriteEndDocument();
            }
            
            Debug.Log($"[DataPipeline] Converted: {Path.GetFileName(csvPath)} -> {Path.GetFileName(xmlPath)}");
        }
        
        /// <summary>
        /// Parse a CSV line handling quoted values.
        /// </summary>
        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                if (c == '"')
                {
                    // Check for escaped quote
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            
            result.Add(current.ToString());
            return result;
        }
        
        /// <summary>
        /// Sanitize string to be a valid XML element name.
        /// </summary>
        private static string SanitizeXmlName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Unknown";
            
            var sb = new StringBuilder();
            
            // First character must be letter or underscore
            char first = name[0];
            if (char.IsLetter(first) || first == '_')
            {
                sb.Append(first);
            }
            else
            {
                sb.Append('_');
            }
            
            // Subsequent characters
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Create a sample CSV file for reference.
        /// </summary>
        private void CreateSampleCsv()
        {
            if (!Directory.Exists(_csvFolderPath))
            {
                Directory.CreateDirectory(_csvFolderPath);
            }
            
            var samplePath = Path.Combine(_csvFolderPath, "SampleCards.csv");
            
            var content = @"Id,Name,Description,Cost,Attack,Health,@Comment
card_001,Fireball,Deal 5 damage to target,3,5,0,Basic damage spell
card_002,Shield,Gain 3 armor,2,0,3,Basic defense card
card_003,Strike,Deal 3 damage,1,3,0,Starter attack card
# This is a comment row - will be ignored
card_004,Heal,Restore 4 health,2,0,0,Basic heal spell
";
            
            File.WriteAllText(samplePath, content, Encoding.UTF8);
            AssetDatabase.Refresh();
            
            Debug.Log($"[DataPipeline] Created sample CSV: {samplePath}");
            EditorUtility.RevealInFinder(samplePath);
        }
    }
    
    /// <summary>
    /// Auto-import CSV files when they change.
    /// </summary>
    public class CsvImportProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets, 
            string[] deletedAssets, 
            string[] movedAssets, 
            string[] movedFromAssetPaths)
        {
            bool hasCsvChanges = false;
            
            foreach (var asset in importedAssets)
            {
                if (asset.EndsWith(".csv") && asset.Contains("DataTables"))
                {
                    hasCsvChanges = true;
                    break;
                }
            }
            
            if (hasCsvChanges)
            {
                // Delay conversion to avoid issues during import
                EditorApplication.delayCall += () =>
                {
                    Debug.Log("[DataPipeline] CSV files changed, auto-converting...");
                    ExcelToXmlConverter.ConvertAllCsvToXml(
                        "Assets/DataTables", 
                        "Assets/Resources/Data"
                    );
                };
            }
        }
    }
}
