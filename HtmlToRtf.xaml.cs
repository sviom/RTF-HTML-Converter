﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using HtmlAgilityPack;

// 빈 페이지 항목 템플릿에 대한 설명은 https://go.microsoft.com/fwlink/?LinkId=234238에 나와 있습니다.

namespace RichTextBoxResearch
{
    /// <summary>
    /// 자체적으로 사용하거나 프레임 내에서 탐색할 수 있는 빈 페이지입니다.
    /// </summary>
    public sealed partial class HtmlToRtf : Page
    {
        public HtmlToRtf()
        {
            this.InitializeComponent();
        }

        private void MoveToMainPage_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage));
        }

        private void ConvertHtmlToRtf_Click(object sender, RoutedEventArgs e)
        {
            var rawHtmlCode = HtmlTextCodeBox.Text;

            var rtfCode  = HtmlToRtfConverter.ParseHtmlText(rawHtmlCode);
            RawTextBlock.Text = rtfCode;
            RichEditBoxFromHtml.TextDocument.SetText(Windows.UI.Text.TextSetOptions.FormatRtf, rtfCode);

            //RichEditBoxFromHtml.TextDocument.SetText(Windows.UI.Text.TextSetOptions.ApplyRtfDocumentDefaults, rawHtmlCode);
        }
    }
}
