﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2008-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Printing
{
    /// <summary>
    /// Interface supported by any of the printing templates
    /// </summary>
    public interface IPrintingTemplate
    {
        /// <summary>
        /// Binds the template to the set of printed pages
        /// </summary>
        /// <param name="lst">The enumerable set of printed pages</param>
        /// <param name="user">The user for whom the pages are printed</param>
        /// <param name="includeImages">True to include images.</param>
        /// <param name="showFooter">True to display the certification/page numbers in the footer.</param>
        void BindPages(IEnumerable<LogbookPrintedPage> lst, Profile user, bool includeImages = false, bool showFooter = true, OptionalColumn[] optionalColumns = null);
    }

    public enum PrintLayoutType { Native, EASA, USA, SACAA, NZ, Glider}

    #region Printing Layout implementations
    public abstract class PrintLayout
    {
        public MyFlightbook.Profile CurrentUser { get; set; }

        /// <summary>
        /// Measures the specified logbookentry and determines if it needs to take up more than one flight row.  
        /// </summary>
        /// <param name="le">The logbook entry to measure</param>
        /// <returns>The # of entries that this should span; default is 1.</returns>
        public abstract int RowHeight(LogbookEntryDisplay le);

        /// <summary>
        /// Does this template support images?
        /// </summary>
        public abstract bool SupportsImages { get; }

        public abstract bool SupportsOptionalColumns { get; }

        /// <summary>
        /// The path to the stylesheet to use for the particular layout
        /// </summary>
        public abstract string CSSPath { get; }

        /// <summary>
        /// Get the layout for the specified type
        /// </summary>
        /// <param name="plt">The type of layout</param>
        /// <returns>The layout object</returns>
        public static PrintLayout LayoutForType(PrintLayoutType plt, MyFlightbook.Profile pf = null)
        {
            switch (plt)
            {
                case PrintLayoutType.Native:
                    return new PrintLayoutNative() { CurrentUser = pf };
                case PrintLayoutType.EASA:
                    return new PrintLayoutEASA() { CurrentUser = pf };
                case PrintLayoutType.USA:
                    return new PrintLayoutUSA() { CurrentUser = pf };
                case PrintLayoutType.SACAA:
                    return new PrintLayoutSACAA() { CurrentUser = pf };
                case PrintLayoutType.NZ:
                    return new PrintLayoutNZ() { CurrentUser = pf };
                case PrintLayoutType.Glider:
                    return new PrintLayoutGlider() { CurrentUser = pf };
                default:
                    throw new ArgumentOutOfRangeException("plt");
            }
        }
    }

    #region concrete layout classes
    public class PrintLayoutNative : PrintLayout
    {
        public override bool SupportsImages { get { return true; } }

        public override bool SupportsOptionalColumns { get { return true; } }

        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException("le");
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            // Signature can be about 3 lines tall
            int sigHeight = le.CFISignatureState == LogbookEntry.SignatureState.None ? 0 : (le.HasDigitizedSig ? 2 : 1);
            int imgHeight = le.FlightImages != null && le.FlightImages.Length > 0 ? 3 : 0;

            // see how many rows of times we have - IF the user shows them
            int times = 0;

            if (CurrentUser != null && CurrentUser.DisplayTimesByDefault)
            {
                times = String.IsNullOrEmpty(le.EngineTimeDisplay) ? 0 : 1;
                times += String.IsNullOrEmpty(le.FlightTimeDisplay) ? 0 : 1;
                times += String.IsNullOrEmpty(le.HobbsDisplay) ? 0 : 1;

                // if there are 1 or 2 rows of times, add 1 to rowheight.  If 3, add 2.
                times = (times + 1) / 2;
            }

            return Math.Max(1 + imgHeight + sigHeight + times, (le.Comment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override string CSSPath { get { return "~/Public/CSS/printNative.css"; } }
    }

    public class PrintLayoutGlider : PrintLayout
    {
        public override bool SupportsImages { get { return false; } }

        public override bool SupportsOptionalColumns { get { return false; } }

        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException("le");
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            return Math.Max(1, (le.Comment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override string CSSPath { get { return "~/Public/CSS/printGlider.css"; } }
    }

    public class PrintLayoutEASA : PrintLayout
    {
        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException("le");
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            return Math.Max(1, (le.Comment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override bool SupportsImages { get { return false; } }

        public override bool SupportsOptionalColumns { get { return false; } }

        public override string CSSPath { get { return "~/Public/CSS/printEASA.css"; } }
    }

    public class PrintLayoutSACAA : PrintLayout
    {
        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException("le");
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            return Math.Max(1, (le.Comment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override bool SupportsImages { get { return false; } }

        public override bool SupportsOptionalColumns { get { return false; } }

        public override string CSSPath { get { return "~/Public/CSS/printSACAA.css"; } }
    }

    public class PrintLayoutNZ : PrintLayout
    {
        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException("le");
            // Very rough computation: look at customproperties + comments, shoot for ~50chars/line, 2 lines/flight, so divide by 100
            return Math.Max(1, (le.Comment.Length + le.CustPropertyDisplay.Length) / 100);
        }

        public override bool SupportsImages { get { return true; } }

        public override bool SupportsOptionalColumns { get { return true; } }

        public override string CSSPath { get { return "~/Public/CSS/printNZ.css"; } }
    }

    public class PrintLayoutUSA : PrintLayout
    {
        public override bool SupportsImages { get { return false; } }

        public override bool SupportsOptionalColumns { get { return true; } }

        public override int RowHeight(LogbookEntryDisplay le)
        {
            if (le == null)
                throw new ArgumentNullException("le");
            // Very rough computation: look at customproperties + comments, shoot for ~120chars/line
            int linesOfText = (int) Math.Ceiling(le.Comment.Length / 120.0) + (int) Math.Ceiling(le.CustPropertyDisplay.Length / 120.0);
            int routeLine = le.Airports.Count() > 2 ? 1 : 0;
            return Math.Max(1, (linesOfText + routeLine + 1) / 2);
        }

        public override string CSSPath { get { return "~/Public/CSS/printUSA.css"; } }
    }
    #endregion

    #endregion

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class PrintingOptions
    {
        public enum PropertySeparatorType { Space, Comma, Semicolon, Newline }

        #region properties
        /// <summary>
        /// Number of flights to print; less than or = 0 for continuous
        /// </summary>
        public int FlightsPerPage { get; set; }

        /// <summary>
        /// Include images when printing?
        /// </summary>
        public bool IncludeImages { get; set; }

        /// <summary>
        /// Layout to use
        /// </summary>
        public PrintLayoutType Layout { get; set; }

        /// <summary>
        /// Properties to exclude from printing
        /// </summary>
        public int[] ExcludedPropertyIDs { get; set; }

        /// <summary>
        /// Character to use to separate properties in print layout
        /// </summary>
        public PropertySeparatorType PropertySeparator { get; set; }

        /// <summary>
        /// Returns the text to use to separate properties (based on PropertySeparator)
        /// </summary>
        public string PropertySeparatorText
        {
            get
            {
                switch (PropertySeparator)
                {
                    default:
                    case PropertySeparatorType.Space:
                        return Resources.LocalizedText.LocalizedSpace;
                    case PropertySeparatorType.Semicolon:
                        return "; ";
                    case PropertySeparatorType.Comma:
                        return ", ";
                    case PropertySeparatorType.Newline:
                        return Environment.NewLine;
                }
            }
        }

        public OptionalColumn[] OptionalColumns { get; set; }
        #endregion

        public PrintingOptions()
        {
            FlightsPerPage = 15;    // default
            IncludeImages = false;
            Layout = PrintLayoutType.Native;
            ExcludedPropertyIDs = new int[0];
            PropertySeparator = PropertySeparatorType.Space;
            OptionalColumns = new OptionalColumn[0];
        }
    }

    /// <summary>
    /// Describes options for saving to PDF
    /// </summary>
    [Serializable]
    public class PDFOptions
    {
        public enum PageOrientation { Landscape, Portrait };
        public enum PageSize { Letter, Legal, Tabloid, Executive, A1, A2, A3, A4, A5, B1, B2, B3, B4, B5};

        #region properties
        /// <summary>
        /// Orientation in which to print
        /// </summary>
        public PageOrientation Orientation { get; set; }

        /// <summary>
        /// Size of paper on which to print
        /// </summary>
        public PageSize PaperSize { get; set; }

        #region Optional margins
        public int? TopMargin { get; set; }
        public int? BottomMargin { get; set; }
        public int? LeftMargin { get; set; }
        public int? RightMargin { get; set; }
        #endregion

        /// <summary>
        /// Text to display in lower left corner of each page
        /// </summary>
        public string FooterLeft { get; set; }

        /// <summary>
        /// Text to display in lower right corner of each page
        /// </summary>
        public string FooterRight { get; set; }

        static public string FooterPageCountArg
        {
            get { return String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.PrintedFooterPageCount, "[page]" /* , "[topage]" */); }
        }
        #endregion

        public PDFOptions()
        {
            Orientation = PageOrientation.Landscape;
            PaperSize = PageSize.Letter;
            FooterLeft = FooterRight = string.Empty;
        }

        /// <summary>
        /// Generates the arguments string to pass to WKHtmlToPDF
        /// </summary>
        /// <param name="inputFile">The name of the source file (html)</param>
        /// <param name="outputFile">The name of the output PDF file</param>
        /// <returns>A string that can be passed as arguments, reflecting the specified options.</returns>
        public string WKHtmlToPDFArguments(string inputFile, string outputFile)
        {
            if (inputFile == null)
                throw new ArgumentNullException("inputFile");
            if (outputFile == null)
                throw new ArgumentNullException("outputFile");
            return String.Format(CultureInfo.InvariantCulture, "--orientation {0} --print-media-type --disable-javascript --footer-font-size 8 -s {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                Orientation.ToString(),
                PaperSize.ToString(),
                TopMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-top {0}", TopMargin.Value) : string.Empty,
                BottomMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-bottom {0}", BottomMargin.Value) : string.Empty,
                LeftMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-left {0}", LeftMargin.Value) : string.Empty,
                RightMargin.HasValue ? String.Format(CultureInfo.InvariantCulture, "--margin-right {0}", RightMargin.Value) : string.Empty,
                String.IsNullOrEmpty(FooterLeft) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "--footer-left \"{0}\"", FooterLeft),
                String.IsNullOrEmpty(FooterRight) ? string.Empty : String.Format(CultureInfo.InvariantCulture, "--footer-right \"{0}\"", FooterRight),
                inputFile,
                outputFile);
        }
    }

    public class PrintingOptionsEventArgs : EventArgs
    {
        public PrintingOptions Options { get; set; }

        public PrintingOptionsEventArgs(PrintingOptions options) : base()
        {
            Options = options;
        }

        public PrintingOptionsEventArgs() : base()
        {
            Options = new PrintingOptions();
        }
    }

    /// <summary>
    /// Represents a collection of subtotals
    /// </summary>
    public class LogbookPrintedPageSubtotalsCollection
    {
        private List<LogbookEntryDisplay> m_list;

        #region properties
        /// <summary>
        /// Title to display for the group
        /// </summary>
        public string GroupTitle { get; set; }

        /// <summary>
        /// Group Type - mostly for sorting
        /// </summary>
        public LogbookEntryDisplay.LogbookRowType GroupType { get; set; }

        /// <summary>
        /// Sorted list of subtotals for the group
        /// </summary>
        public IEnumerable<LogbookEntryDisplay> Subtotals { get { return m_list; } }

        /// <summary>
        /// Number of subtotals for this group
        /// </summary>
        public int SubtotalCount { get { return m_list.Count; } }
        #endregion

        #region Constructors
        public LogbookPrintedPageSubtotalsCollection(LogbookEntryDisplay.LogbookRowType groupType, string title, IEnumerable<LogbookEntryDisplay> lst)
        {
            GroupType = groupType;
            GroupTitle = title;
            m_list = new List<LogbookEntryDisplay>(lst);
            m_list.Sort((le1, le2) => { return le1.EffectiveCatClass.CompareTo(le2.EffectiveCatClass); });
        }
        #endregion
    }

    /// <summary>
    /// Represents a set of flights and subtotals for a printed page
    /// </summary>
    public class LogbookPrintedPage
    {
        #region properties
        /// <summary>
        /// The flights for the page
        /// </summary>
        public IEnumerable<LogbookEntryDisplay> Flights { get; set; }

        /// <summary>
        /// Page number for this printed page
        /// </summary>
        public int PageNum { get; set; }

        /// <summary>
        /// Total # of pages to print
        /// </summary>
        public int TotalPages { get; set; }

        public IEnumerable<LogbookPrintedPageSubtotalsCollection> Subtotals
        {
            get
            {
                List<LogbookPrintedPageSubtotalsCollection> lst = new List<LogbookPrintedPageSubtotalsCollection>();
                if (TotalsThisPage.Count > 0)
                    lst.Add(new LogbookPrintedPageSubtotalsCollection(LogbookEntryDisplay.LogbookRowType.PageTotal, Resources.LogbookEntry.PrintTotalsThisPage, SortedSubtotals(TotalsThisPage)));
                if (TotalsPreviousPages.Count > 0)
                    lst.Add(new LogbookPrintedPageSubtotalsCollection(LogbookEntryDisplay.LogbookRowType.PreviousTotal, Resources.LogbookEntry.PrintTotalsPreviousPage, SortedSubtotals(TotalsPreviousPages)));
                if (RunningTotals.Count > 0)
                    lst.Add(new LogbookPrintedPageSubtotalsCollection(LogbookEntryDisplay.LogbookRowType.RunningTotal, Resources.LogbookEntry.PrintTotalsRunning, SortedSubtotals(RunningTotals)));

                return lst;
            }
        }

        #region internal properties
        /// <summary>
        /// Totals from this page, striped by category/class
        /// </summary>
        protected IDictionary<string, LogbookEntryDisplay> TotalsThisPage { get; set; }

        /// <summary>
        /// Totals from previous pages, striped by category/class
        /// </summary>
        protected IDictionary<string, LogbookEntryDisplay> TotalsPreviousPages { get; set; }

        /// <summary>
        /// Running totals, striped by category/class
        /// </summary>
        protected IDictionary<string, LogbookEntryDisplay> RunningTotals { get; set; }
        #endregion
        #endregion

        #region Constructors
        public LogbookPrintedPage()
        {
            Flights = new List<LogbookEntryDisplay>();
            TotalsThisPage = new Dictionary<string, LogbookEntryDisplay>();
            TotalsPreviousPages = new Dictionary<string, LogbookEntryDisplay>();
            RunningTotals = new Dictionary<string, LogbookEntryDisplay>();
        }
        #endregion

        #region Subtotals collections
        private List<LogbookEntryDisplay> SortedSubtotals(IDictionary<string, LogbookEntryDisplay> d)
        {
            List<LogbookEntryDisplay> lstResult = new List<LogbookEntryDisplay>();
            foreach (string key in d.Keys)
                lstResult.Add(d[key]);
            lstResult.Sort((led1, led2) => { return led1.EffectiveCatClass.CompareTo(led2.EffectiveCatClass); });
            return lstResult;
        }
        #endregion

        /// <summary>
        /// Inserts subtotals into an enumerable set of flights, returning an enumerable set of LogbookPrintedPages.
        /// </summary>
        /// <param name="lstIn">The input set of flights.  Should be ALL RowType=Flight and should have rowheight property set</param>
        /// <param name="pageSize">Max # of flights per table to subtotal; flights with rowheight > 1 will take up more rows</param>
        /// <param name="optionalColumns">Any optional columns to add</param>
        /// <returns>A new enumerable with per-page subtotals and (optional) running totals</returns>
        public static IEnumerable<LogbookPrintedPage> Paginate(IEnumerable<LogbookEntryDisplay> lstIn, int pageSize, OptionalColumn[] optionalColumns)
        {
            if (lstIn == null)
                throw new ArgumentNullException("lstIn");
            int cIn = lstIn.Count();
            if (cIn == 0)
                return new LogbookPrintedPage[0];

            // For speed, cache the names of each category/class
            Dictionary<int, string> dictCatClasses = new Dictionary<int, string>();
            foreach (CategoryClass cc in CategoryClass.CategoryClasses())
                dictCatClasses.Add(cc.IDCatClassAsInt, cc.CatClass);

            List<LogbookPrintedPage> lstOut = new List<LogbookPrintedPage>();

            Dictionary<string, LogbookEntryDisplay> dictPageTotals = null, dictPreviousTotals = new Dictionary<string, LogbookEntryDisplay>(), dictRunningTotals = new Dictionary<string, LogbookEntryDisplay>();
            List<LogbookEntryDisplay> lstFlightsThisPage = null;
            LogbookPrintedPage currentPage = null;

            int flightIndexOnPage = 0;
            int index = 0;
            int pageNum = 0;

            foreach (LogbookEntryDisplay led in lstIn)
            {
                led.OptionalColumns = optionalColumns;
                if ((pageSize > 0 && flightIndexOnPage >= pageSize) || currentPage == null)   // need to start a new page.
                {
                    flightIndexOnPage = 0;  // reset
                    dictPageTotals = new Dictionary<string, LogbookEntryDisplay>();
                    // COPY the running totals to the new previous totals, since AddFrom modifies the object, 
                    dictPreviousTotals = new Dictionary<string, LogbookEntryDisplay>();
                    Dictionary<string, LogbookEntryDisplay> dictNewRunningTotals = new Dictionary<string, LogbookEntryDisplay>();
                    foreach (string szKeySrc in dictRunningTotals.Keys)
                    {
                        LogbookEntryDisplay ledRunningNew = JsonConvert.DeserializeObject<LogbookEntryDisplay>(JsonConvert.SerializeObject(dictRunningTotals[szKeySrc]));
                        ledRunningNew.RowType = LogbookEntryDisplay.LogbookRowType.PreviousTotal;
                        dictPreviousTotals[szKeySrc] = ledRunningNew;
                        ledRunningNew = JsonConvert.DeserializeObject<LogbookEntryDisplay>(JsonConvert.SerializeObject(dictRunningTotals[szKeySrc]));
                        ledRunningNew.RowType = LogbookEntryDisplay.LogbookRowType.RunningTotal;
                        dictNewRunningTotals[szKeySrc] = ledRunningNew;
                    }
                    dictRunningTotals = dictNewRunningTotals;  // set up for the new page to pick up where the last one left off...
                    lstFlightsThisPage = new List<LogbookEntryDisplay>();
                    currentPage = new LogbookPrintedPage() { RunningTotals = dictRunningTotals, TotalsPreviousPages = dictPreviousTotals, TotalsThisPage = dictPageTotals, Flights = lstFlightsThisPage, PageNum = ++pageNum };

                    lstOut.Add(currentPage);
                }

                flightIndexOnPage += led.RowHeight;

                string szCatClassKey = dictCatClasses[led.EffectiveCatClass];   // should never not be present!!

                led.Index = ++index;

                // Add the flight to the page
                lstFlightsThisPage.Add(led);

                // And add the flight to the page catclass totals and running catclass totals
                if (pageSize > 0)
                {
                    if (!dictPageTotals.ContainsKey(szCatClassKey))
                        dictPageTotals[szCatClassKey] = new LogbookEntryDisplay() { RowType = LogbookEntryDisplay.LogbookRowType.PageTotal, CatClassDisplay = szCatClassKey, OptionalColumns = optionalColumns };
                    dictPageTotals[szCatClassKey].AddFrom(led);
                }
                if (!dictRunningTotals.ContainsKey(szCatClassKey))
                    dictRunningTotals[szCatClassKey] = new LogbookEntryDisplay() { RowType = LogbookEntryDisplay.LogbookRowType.RunningTotal, CatClassDisplay = szCatClassKey, OptionalColumns = optionalColumns };
                dictRunningTotals[szCatClassKey].AddFrom(led);
            }

            // Assign page number, and index totals
            foreach (LogbookPrintedPage lpp in lstOut)
            {
                // And add unstriped totals as needed
                ConsolidateTotals(lpp.TotalsThisPage, LogbookEntryDisplay.LogbookRowType.PageTotal, optionalColumns);
                ConsolidateTotals(lpp.TotalsPreviousPages, LogbookEntryDisplay.LogbookRowType.PreviousTotal, optionalColumns);
                ConsolidateTotals(lpp.RunningTotals, LogbookEntryDisplay.LogbookRowType.RunningTotal, optionalColumns);

                lpp.TotalPages = pageNum;
                int iTotal = 0;
                foreach (LogbookEntryDisplay lep in lpp.TotalsThisPage.Values)
                    lep.Index = iTotal++;
                iTotal = 0;
                foreach (LogbookEntryDisplay lep in lpp.TotalsPreviousPages.Values)
                    lep.Index = iTotal++;
                iTotal = 0;
                foreach (LogbookEntryDisplay lep in lpp.RunningTotals.Values)
                    lep.Index = iTotal++;

            }

            return lstOut;
        }

        private static void ConsolidateTotals(IDictionary<string, LogbookEntryDisplay> d, LogbookEntryDisplay.LogbookRowType rowType, OptionalColumn[] optionalColumns)
        {
            if (d == null || d.Count <= 1)
                return;

            LogbookEntryDisplay ledAll = new LogbookEntryDisplay() { RowType = rowType, OptionalColumns = optionalColumns };
            foreach (LogbookEntryDisplay led in d.Values)
                ledAll.AddFrom(led);
            ledAll.CatClassDisplay = Resources.LogbookEntry.PrintTotalsAllCatClass;
            d[Resources.LogbookEntry.PrintTotalsAllCatClass] = ledAll;
        }
    }
}