using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace RichTextBoxResearch
{
    /// <summary>
    /// Html 텍스트를 RTF Format으로 변환해주는 컨버터
    /// </summary>
    public class HtmlToRtfConverter
    {
        public const string DefaultHeader = "{\\rtf1 \\fbidis \\ansi ";
        public const string DefaultFooter = " }";

        public const string AttributeMatch = @"[_a-zA-z_0-9\-]+[:]+[_a-zA-z_0-9\-\s]+[;]";

        public static List<CssClass> CssClasses { get; set; }

        public static string ParseHtmlText(string htmlText)
        {
            var builder = new StringBuilder();
            builder.Append(DefaultHeader);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlText);

            var htmlNode = htmlDoc.DocumentNode;

            CssClasses = new List<CssClass>();
            // Internal CSS 검색해서 미리 만들어 놓기
            var styleData = htmlNode.Descendants("style");
            var classAllMatch = @"([\.#][_A-Za-z0-9\-]+)[^}]*{[^}]*}";          // 클래스 전체
            var classNameMatch = @"[\.#][_A-Za-z0-9\-]+[^}]";                   // 클래스 이름만
            //var attributeMatch = @"[_a-zA-z_0-9\-]+[:]+[_a-zA-z_0-9\-\s]+[;]";  // Attribute 및 값 까지
            foreach (var item in styleData)
            {
                var styleInnerText = item.InnerText;
                var classRegex = new Regex(classAllMatch);
                var classText = classRegex.Matches(styleInnerText);

                // Class 만들기
                foreach (Match m in classText)
                {
                    //Debug.WriteLine("'{0}' found at index {1}.", m.Value, m.Index);
                    var classInnerText = m.Value;

                    var classNameReg = new Regex(classNameMatch);
                    var attrReg = new Regex(AttributeMatch);

                    var matchResult = classNameReg.Matches(classInnerText);
                    var attrResult = attrReg.Matches(classInnerText);

                    var cssClass = new CssClass() { ClassName = matchResult[0].Value.Trim().Replace(".", "").Replace("#", "") };
                    // Attribute 만들기
                    foreach (Match attrItem in attrResult)
                    {
                        var dividerIndex = attrItem.Value.IndexOf(":");       // Attribute에서 :(콜론)으로 나뉘는 부분의 Index
                        var attr = attrItem.Value.Substring(0, dividerIndex).Trim();
                        var attrVal = attrItem.Value
                                            .Substring(dividerIndex, attrItem.Value.Count() - dividerIndex)
                                            .Trim()
                                            .Replace(";", "").Replace(":", "")
                                            .Split(null);

                        var cssAttritue = new CssAttritue() { AttributeName = attr };
                        cssClass.CssAttritues.Add(cssAttritue);
                        // Value 만들기(Attribute value)
                        foreach (string valueItem in attrVal)
                        {
                            if (!string.IsNullOrWhiteSpace(valueItem))
                            {
                                var cssValue = new CssValue() { Value = valueItem };
                                cssAttritue.CssValues.Add(cssValue);
                            }
                        }
                    }
                    CssClasses.Add(cssClass);
                }
            }

            // HTML Node들 돌면서 CSS 적용 및 텍스트 만들기
            var htmlAllNodes = htmlNode.Descendants();
            foreach (var nodeItem in htmlAllNodes)
            {
                if (nodeItem.Name.Contains("div") || nodeItem.Name.Contains("span"))
                {
                    SetValueToRtf(nodeItem, ref builder);
                    builder.Append(" \\par ");
                }
            }

            builder.Append(DefaultFooter);
            CssClasses.Clear();
            return builder.ToString();
        }

        public static void SetValueToRtf(HtmlNode htmlNode, ref StringBuilder builder)
        {
            foreach (var attribute in htmlNode.Attributes)
            {
                if (attribute.Name == "style")
                {
                    var cssAttritues = new List<CssAttritue>();
                    cssAttritues = GetAttributesInfo(attribute.Value);

                    // 적용
                    foreach (var cssAttritue in cssAttritues)
                    {
                        foreach (var cssValue in cssAttritue.CssValues)
                        {
                            var rtfTuple = RtfSpec.GetRtfCodeFromCss(cssAttritue.AttributeName, cssValue.Value);

                            // 적용할 텍스트가 있는지 검사해서 있으면 적용
                            if (htmlNode.FirstChild.Name.Equals("#text") && !string.IsNullOrEmpty(htmlNode.FirstChild.InnerText.Trim()))
                            {
                                var valueText = new StringBuilder(htmlNode.FirstChild.InnerText);

                                valueText.Insert(0, rtfTuple.header);
                                valueText.Append(rtfTuple.footer);

                                builder.Append(valueText);
                            }
                        }
                    }
                }
                else if (attribute.Name == "class")
                {
                    var rtfTuples = GetRtfCodeFromClasses(attribute.Value);

                    // 적용할 텍스트가 있는지 검사해서 있으면 적용
                    if (htmlNode.FirstChild.Name.Equals("#text") && !string.IsNullOrEmpty(htmlNode.FirstChild.InnerText.Trim()))
                    {
                        var valueText = new StringBuilder(htmlNode.FirstChild.InnerText);
                        foreach (var rtfTuple in rtfTuples)
                        {

                            valueText.Insert(0, rtfTuple.header);
                            valueText.Append(rtfTuple.footer);
                        }
                        builder.Append(valueText);
                    }
                }
            }
        }

        public static List<CssAttritue> GetAttributesInfo(string attributeValue)
        {
            var cssAttritues = new List<CssAttritue>();
            // Attribute Raw string 분해
            var attrReg = new Regex(AttributeMatch);
            var attrResult = attrReg.Matches(attributeValue);
            // Attribute 만들기
            foreach (Match attrItem in attrResult)
            {
                var dividerIndex = attrItem.Value.IndexOf(":");       // Attribute에서 :(콜론)으로 나뉘는 부분의 Index
                var attr = attrItem.Value.Substring(0, dividerIndex).Trim();
                var attrVal = attrItem.Value
                                    .Substring(dividerIndex, attrItem.Value.Count() - dividerIndex)
                                    .Trim()
                                    .Replace(";", "").Replace(":", "")
                                    .Split(null);

                var cssAttritue = new CssAttritue() { AttributeName = attr };
                //cssClass.CssAttritues.Add(cssAttritue);
                cssAttritues.Add(cssAttritue);
                // Value 만들기(Attribute value)
                foreach (string valueItem in attrVal)
                {
                    if (!string.IsNullOrWhiteSpace(valueItem))
                    {
                        var cssValue = new CssValue() { Value = valueItem };
                        cssAttritue.CssValues.Add(cssValue);
                    }
                }
            }

            return cssAttritues;
        }

        public static List<(string header, string footer)> GetRtfCodeFromClasses(string _className)
        {
            var ps = new List<(string header, string footer)>();
            var rtfCode = (header: "", footer: "");
            foreach (var cssClass in CssClasses)
            {
                if (cssClass.ClassName.Equals(_className))
                {
                    foreach (var cssAttritue in cssClass.CssAttritues)
                    {
                        foreach (var cssValue in cssAttritue.CssValues)
                        {
                            rtfCode = RtfSpec.GetRtfCodeFromCss(cssAttritue.AttributeName, cssValue.Value);
                            ps.Add(rtfCode);
                        }
                    }
                }
            }

            return ps;
        }
    }

    public class CssClass
    {
        public string ClassName { get; set; }
        public List<CssAttritue> CssAttritues { get; set; } = new List<CssAttritue>();
    }

    public class CssAttritue
    {
        public string AttributeName { get; set; }
        public List<CssValue> CssValues { get; set; } = new List<CssValue>();
    }

    public class CssValue
    {
        public string Value { get; set; }
    }

}
