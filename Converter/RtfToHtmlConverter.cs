using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace RichTextBoxResearch.Converter
{
    public class RtfToHtmlConverter
    {
        public static string DefaultHeader = "<!DOCTYPE html>";

        public static async Task<string> ParseRtfText(string rawText = "")
        {
            string htmlText = string.Empty;
            string test = string.IsNullOrEmpty(rawText) ? await ConverterHelper.GetFileFromAppAsync().ConfigureAwait(false) : rawText;
            char[] textCharArr = test.Replace("\r", "").Replace("\n", "").ToCharArray();
            int startPoint = 0;

            var attributeList = new List<string>();
            string attribute = "";
            for (var i = 0; i < textCharArr.Length; i++)
            {
                var item = textCharArr[i];
                switch (item)
                {
                    case '{':
                    case '}':
                    case '\r':
                    case '\n':
                        break;
                    //case '\\':
                    case ' ':
                        // Attribute가 아무것도 없으면 쓸모없는 공간
                        if (!string.IsNullOrEmpty(attribute))
                        {
                            if (attribute[0].Equals('\\'))
                            {
                                // RTF Format과 일반 단어 사이에 위치한 공백
                                attributeList.Add(attribute);
                                attribute = string.Empty;
                            }
                            else
                            {
                                // 일반 단어 사이에 위치한 공백
                                if (!string.IsNullOrWhiteSpace(attribute))
                                    attribute += item;
                            }
                        }
                        break;
                    default:
                        if (!string.IsNullOrEmpty(attribute))
                        {
                            // 나중에 해당 Attribute를 해당 Attribute의 단어가 RTF Format 예약어로 사용중인지
                            // 검사해서 맞으면 value가 아닌 것으로 체크하는 로직 필요
                            // item항목 전의 앞의 단어는 사용자 입력 문구
                            if (item == '\\' && !attribute.Contains("\\"))
                            {
                                // 일반 단어
                                attribute = attribute.TrimStart().TrimEnd();
                                attribute = attribute.Insert(0, "<itisdesignemval>");
                                attribute = attribute.Insert(attribute.Length, "</itisdesignemval>");
                                attributeList.Add(attribute);
                                attribute = string.Empty;
                            }
                            else if (item == '\\')
                            {
                                attributeList.Add(attribute);
                                attribute = string.Empty;
                            }
                            else
                            {
                                var reservedWorld = attribute.Replace("\\", "");
                                // 예약어 이면 
                                if (!string.IsNullOrEmpty(RtfSpec.GetHtmlFromRtfCode(reservedWorld)))
                                {
                                    // \b \b0 같은 쌍으로 묶인 항목
                                    if (item != '0')
                                    {
                                        attributeList.Add(attribute);
                                        attribute = string.Empty;
                                    }
                                    // none로 끝나는 항목
                                    //if(item != 'n' && item != 'o' && item != 'e') { }
                                }
                            }
                        }

                        // 다시 시작
                        attribute += item;
                        attribute = attribute.TrimStart().TrimEnd();
                        break;
                }
            }

            htmlText += DefaultHeader;
            htmlText += "<html>";
            htmlText += "<head>";
            htmlText += "<title>RTF to Html Test page</title>";
            htmlText += "</head>";
            htmlText += "<body>";

            // 시작점 찾기
            for (int i = 0; i < attributeList.Count; i++)
            {
                if (Regex.IsMatch(attributeList[i], @"\\lang[0-9*]"))
                    startPoint = i;
            }

            // 본문 쓰기
            for (int i = startPoint; i < attributeList.Count; i++)
            {
                var item = attributeList[i];
                if (item.Contains("<itisdesignemval>"))     // 값
                {
                    var beforeItem = attributeList[i - 1].Replace("\\", "");
                    var valueText = item.Replace("<itisdesignemval>", "").Replace("</itisdesignemval>", "");
                    //htmlText += RtfSpec.GetHtmlFromRtfCode(beforeItem, valueText);
                    htmlText += valueText;
                }
                else
                {
                    item = item.Replace("\\", "");
                    htmlText += RtfSpec.GetHtmlFromRtfCode(item);
                }
            }

            htmlText += "</body>";
            htmlText += "</html>";

            return htmlText;
        }

        public static void ParseHtmlText()
        {
            string htmlText = "";
        }
    }

    public class ConverterHelper
    {
        public static async Task<string> GetFileFromAppAsync(string fileName = "")
        {
            var uri = new Uri("ms-appx:///TestFiles/RTF_to_HtmlTest.txt");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            string text = await FileIO.ReadTextAsync(file);

            return text;
        }
    }

}
