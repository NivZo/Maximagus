using Godot;
using System;

public static class RichTextLabelExtensions
{
    public static RichTextLabel FitTextToSize(this RichTextLabel rtb, bool forceNumberOfLines = true, int maxFontSize = 60, int minFontSize = 6, int fontSizeStep = 4)
    {
        if (rtb == null)
        {
            return rtb;
        }

        rtb.AutowrapMode = TextServer.AutowrapMode.Word;
        rtb.FitContent = false;

        float targetHeight = rtb.Size.Y;
        int targetLineCount = -1;
        if (forceNumberOfLines)
        {
            targetLineCount = rtb.Text?.Split('\n').Length ?? 1;
            targetLineCount = Math.Max(targetLineCount, 1);
        }

        int currentAttemptedFontSize = maxFontSize;
        while (currentAttemptedFontSize >= minFontSize)
        {
            rtb.AddThemeFontSizeOverride("normal_font_size", currentAttemptedFontSize);
            rtb.AddThemeFontSizeOverride("bold_font_size", currentAttemptedFontSize);
            float contentHeight = rtb.GetContentHeight();
            int currentRenderedLineCount = rtb.GetLineCount();
            bool heightFits = contentHeight <= targetHeight;
            bool linesFit = true;

            if (forceNumberOfLines)
            {
                linesFit = currentRenderedLineCount <= targetLineCount;
            }

            if (heightFits && linesFit)
            {
                return rtb;
            }

            currentAttemptedFontSize -= fontSizeStep;
        }

        rtb.AddThemeFontSizeOverride("normal_font_size", minFontSize);
        rtb.AddThemeFontSizeOverride("bold_font_size", minFontSize);
        return rtb;
    }
}