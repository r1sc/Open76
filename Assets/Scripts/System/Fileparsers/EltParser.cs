using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Assets.Scripts.System.Fileparsers
{
    public class EltParser
    {
        public static List<ETbl> ParseElt(string fileName)
        {
            List<ETbl> etblList = new List<ETbl>();

            using (FastBinaryReader br = VirtualFilesystem.Instance.GetFileStream(fileName))
            {
                string eltText = Encoding.ASCII.GetString(br.Data, (int)br.Position, br.Length - (int)br.Position);
                string[] lines = eltText.Split('\n');

                ETbl currentEtbl = null;

                int lineCount = lines.Length;
                for (int i = 0; i < lineCount; ++i)
                {
                    string[] lineParts = lines[i].Trim().Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    int linePartCount = lineParts.Length;

                    if (linePartCount == 2)
                    {
                        if (currentEtbl != null)
                        {
                            currentEtbl.ItemCount = (uint)currentEtbl.Items.Count;
                            etblList.Add(currentEtbl);
                        }

                        currentEtbl = new ETbl
                        {
                            IsReferenceImage = lineParts[0] == "dst",
                            MapFile = lineParts[1],
                            Items = new Dictionary<string, ETbl.ETblItem>()
                        };
                    }
                    else if (linePartCount == 6)
                    {
                        if (currentEtbl == null)
                        {
                            Debug.LogErrorFormat("Received ETBL data before ETBL was declared.");
                            continue;
                        }

                        ETbl.ETblItem item = new ETbl.ETblItem
                        {
                            Name = lineParts[1],
                            XOffset = int.Parse(lineParts[2]),
                            YOffset = int.Parse(lineParts[3]),
                            Width = int.Parse(lineParts[4]),
                            Height = int.Parse(lineParts[5])
                        };

                        currentEtbl.Items.Add(item.Name, item);
                    }
                    else if (linePartCount > 0)
                    {
                        Debug.LogErrorFormat("Unexpected word count in ELT file on line {0}. Found {1} words.'", i, linePartCount);
                    }
                }

                if (currentEtbl != null)
                {
                    currentEtbl.ItemCount = (uint)currentEtbl.Items.Count;
                    etblList.Add(currentEtbl);
                }
            }

            return etblList;
        }
    }
}
