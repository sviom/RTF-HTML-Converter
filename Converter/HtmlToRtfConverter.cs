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
        public const string DefaultHeader = "{\\rtf1 ";
        public const string DefaultFooter = " }";

        public static string ParseHtmlText(string htmlText)
        {
            var builder = new StringBuilder();
            builder.Append(DefaultHeader);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlText);

            var htmlNode = htmlDoc.DocumentNode;

            var cssClasses = new List<CssClass>();
            // Internal CSS 검색해서 미리 만들어 놓기
            var styleData = htmlNode.Descendants("style");
            var classAllMatch = @"([\.#][_A-Za-z0-9\-]+)[^}]*{[^}]*}";          // 클래스 전체
            var classNameMatch = @"[\.#][_A-Za-z0-9\-]+[^}]";                   // 클래스 이름만
            var attributeMatch = @"[_a-zA-z_0-9\-]+[:]+[_a-zA-z_0-9\-\s]+[;]";  // Attribute 및 값 까지
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
                    var attrReg = new Regex(attributeMatch);

                    var matchResult = classNameReg.Matches(classInnerText);
                    var attrResult = attrReg.Matches(classInnerText);

                    var cssClass = new CssClass() { ClassName = matchResult[0].Value.Trim().Replace(".","").Replace("#", "") };
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
                    cssClasses.Add(cssClass);
                }
            }

            // HTML Node들 돌면서 CSS 적용 및 텍스트 만들기
            var htmlAllNodes = htmlNode.Descendants();
            foreach (var nodeItem in htmlAllNodes)
            {
                var nodeClassList = nodeItem.GetClasses();
                foreach (var attribute in nodeItem.Attributes)
                {
                    // 클래스면 기존 클래스의 내용들을 가져와서 적용 시켜 주어야 한다.
                    if (attribute.Name == "class")
                    {
                        var nodeValue = nodeItem.InnerText;
                        builder.Append(GetRtfCodeFromClass(cssClasses, attribute.Value, new StringBuilder(nodeValue)));
                    }
                    else
                    {
                        //builder.Append(RtfSpec.GetRtfCodeFromCss(attribute.Name, nodeItem.InnerText));
                    }
                }
            }

            builder.Append(DefaultFooter);
            return builder.ToString();
        }

        public static string GetRtfCodeFromClass(List<CssClass> cssClasses, string _className, StringBuilder nodeValue)
        {
            foreach (var cssClass in cssClasses)
            {
                if (cssClass.ClassName.Equals(_className))
                {
                    foreach (var cssAttritue in cssClass.CssAttritues)
                    {
                        foreach (var cssValue in cssAttritue.CssValues)
                        {
                            var rtfTuple = RtfSpec.GetRtfCodeFromCss(cssAttritue.AttributeName, cssValue.Value);
                            nodeValue.Insert(0, rtfTuple.header);
                            nodeValue.Append(rtfTuple.footer);
                        }
                    }
                }
            }

            return nodeValue.ToString();
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
