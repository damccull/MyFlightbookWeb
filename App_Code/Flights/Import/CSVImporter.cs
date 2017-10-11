﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using JouniHeikniemi.Tools.Text;

/******************************************************
 * 
 * Copyright (c) 2008-2016 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.ImportFlights
{
    [Serializable]
    public class CSVImporter
    {
        private ImportContext m_ImportContext = null;
        private List<AircraftImportMatchRow> m_missingAircraft = new List<AircraftImportMatchRow>();

        #region properties
        /// <summary>
        /// Result of last import file run
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Any errors in the last run?
        /// </summary>
        public bool HasErrors { get; set; }

        /// <summary>
        /// The set of flights to import.
        /// </summary>
        public List<LogbookEntry> FlightsToImport { get; set; }

        /// <summary>
        /// The aircraft that were encountered that are not yet in the user's profile
        /// </summary>
        public IEnumerable<AircraftImportMatchRow> MissingAircraft
        {
            get { return m_missingAircraft; }
            set { m_missingAircraft = new List<AircraftImportMatchRow>(value); }
        }

        /// <summary>
        /// The primary name used for the tail number column
        /// </summary>
        public static string TailNumberColumnName
        {
            get { return colTail[0]; }
        }

        /// <summary>
        /// The primary name used for the model column
        /// </summary>
        public static string ModelColumnName
        {
            get { return colModelName[0]; }
        }
        #endregion

        //  TODO: Pull these out of here so that we can reference them from externalformat.
        #region column names
        /*
         * The following are the column headers for known columns for import.  Each is an array of headers, in prioritized order.  
         * So, for example, the comments field has "Comments" as the highest priority column; if that is not found, then "Remarks" will be used next, and so forth.
        */
        private static string[] colFlightID = { "Flight ID" };
        private static string[] colDate = { "Date" };
        private static string[] colTail = { "Tail Number", "Registration", "Aircraft ID", "Ident" };
        private static string[] colAircraftID = { "Aircraft ID" };
        private static string[] colTotal = { "Total Flight Time", "Total Time", "TotalDuration", "Flt Time" };
        private static string[] colApproaches = { "Approaches", "NumApproaches", "Inst App (D/N)", "Inst App", "IAP" };
        private static string[] colHold = { "Hold", "Holds", "Holding" };
        private static string[] colLandings = { "Landings" };
        private static string[] colNightLandings = { "FS Night Landings", "flight_nightLandings", "Night Ldg", "Night Ldgs", "Ngt Ldgs", "Full-Stop Night Landings" };
        private static string[] colFullStopLandings = { "FS Day Landings", "flight_dayLandings", "Day Ldg", "Day Ldgs", "Full-Stop Day Landings" };
        private static string[] colCrossCountry = { "X-Country", "flight_crossCountry", "XCountry", "XC", "X CNTY", "X/Ctry", "X/C" };
        private static string[] colNight = { "Night", "flight_night" };
        private static string[] colIMC = { "IMC", "flight_actualInstrument", "Actual Inst" };
        private static string[] colSimIFR = { "Simulated Instrument", "flight_simulatedInstrument", "Hood", "Sim Inst" };
        private static string[] colGroundSim = { "Ground Simulator", "flight_simulator", "Sim/FTD" };
        private static string[] colDual = { "Dual Received", "flight_dualReceived", "Dualreceived", "Dual Recd" };
        private static string[] colCFI = { "CFI", "flight_dualGiven", "DualGiven", "Dual Given" };
        private static string[] colSIC = { "SIC", "flight_sic" };
        private static string[] colPIC = { "PIC", "flight_pic" };
        private static string[] colRoute = { "Route", "flight_route", "Via" };
        private static string[] colFrom = { "From", "flight_from", "Departure" };
        private static string[] colTo = { "To", "flight_to", "Arrival" };
        private static string[] colComment = { "Comments", "Remarks" };
        private static string[] colCatClassOverride = { "CatClassOverride" };
        private static string[] colEngineStart = { "Engine Start" };
        private static string[] colEngineEnd = { "Engine End" };
        private static string[] colFlightStart = { "Flight Start" };
        private static string[] colFlightEnd = { "Flight End" };
        private static string[] colHobbsStart = { "Hobbs Start" };
        private static string[] colHobbsEnd = { "Hobbs End" };
        private static string[] colModelName = { "Model", "Aircraft Type", "MakeModel", "MAKE & MODEL" };

        /// <summary>
        /// Common aliases for property names
        /// TODO: THIS CURRENTLY IS NOT LOCALIZABLE; SHOULD ADD THE LOCALIZED NAME OF THE PROPERTY TO THE LIST!
        /// </summary>
        private static Dictionary<string, string[]> PropNameAliases = new Dictionary<string, string[]>()
        {
            { "Solo Time", new string[] {"Solo Time", "flight_solo", "Solo"}},
            { "Name of PIC", new string[] {"Name of PIC", "flight_selectedCrewPIC", "PIC/P1 Crew"}},
            { "Name of SIC", new string[] {"Name of SIC", "SIC/P2 Crew"}},
            { "Takeoffs - Night", new string[] {"Takeoffs - Night", "flight_nightTakeoffs", "Night T/O"}},
            { "Landings - Water", new string[] {"Landings - Water", "flight_waterLandings"}},
            { "Takeoffs - Water", new string[] {"Takeoffs - Water", "flight_waterTakeoffs"}},
            {"Night Vision - Landing", new string[] {"Night Vision - Landing", "flight_nightVisionGoggleLandings"}},
            {"Night Vision - Takeoff", new string[] {"Night Vision - Takeoff", "flight_nightVisionGoggleTakeoffs"}},
            {"Go-arounds", new string[] {"Go-arounds", "flight_goArounds"}},
            {"Duty Time End (UTC)", new string[] {"Duty Time End (UTC)", "flight_offDutyTime"}},
            {"Duty Time Start (UTC)", new string[] {"Duty Time Start (UTC)", "flight_onDutyTime"}},
            {"Part 91 Flight", new string[] {"Part 91 Flight", "flight_faaPart91"}},
            {"Part 121 Flight", new string[] {"Part 121 Flight", "flight_faaPart121"}},
            {"Part 135 Flight", new string[] {"Part 135 Flight", "flight_faaPart135"}},
            {"Tachometer End", new string[] {"Tachometer End", "Tach In"}},
            {"Tachometer Start", new string[] {"Tachometer Start", "Tach Out"}},
            {"Student Name", new string[] {"Student Name", "Student"}}
        };

        public static void InitializeDataTable(DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException("dt");

            dt.Columns.Add(new DataColumn(colFlightID[0], typeof(int)));
            dt.Columns.Add(new DataColumn(colDate[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colTail[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colAircraftID[0], typeof(int)));
            dt.Columns.Add(new DataColumn(colTotal[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colApproaches[0], typeof(Int32)));
            dt.Columns.Add(new DataColumn(colHold[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colLandings[0], typeof(Int32)));
            dt.Columns.Add(new DataColumn(colNightLandings[0], typeof(Int32)));
            dt.Columns.Add(new DataColumn(colFullStopLandings[0], typeof(Int32)));
            dt.Columns.Add(new DataColumn(colCrossCountry[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colNight[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colIMC[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colSimIFR[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colGroundSim[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colDual[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colCFI[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colSIC[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colPIC[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colRoute[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colComment[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colCatClassOverride[0], typeof(Int32)));
            dt.Columns.Add(new DataColumn(colEngineStart[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colEngineEnd[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colFlightStart[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colFlightEnd[0], typeof(string)));
            dt.Columns.Add(new DataColumn(colHobbsStart[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colHobbsEnd[0], typeof(decimal)));
            dt.Columns.Add(new DataColumn(colModelName[0], typeof(string)));
        }

        public static void WriteEntryToDataTable(LogbookEntry le, DataTable dt)
        {
            if (dt == null)
                throw new ArgumentNullException("dt");
            if (le == null)
                throw new ArgumentNullException("le");

            DataRow dr = dt.NewRow();
            dt.Rows.Add(dr);

            dr[colFlightID[0]] = le.FlightID;
            dr[colDate[0]] = le.Date.ToShortDateString();
            dr[colTail[0]] = le.TailNumDisplay;
            dr[colAircraftID[0]] = le.AircraftID;
            dr[colTotal[0]] = le.TotalFlightTime;
            dr[colApproaches[0]] = le.Approaches;
            dr[colHold[0]] = le.fHoldingProcedures ? 1.FormatBooleanInt() : string.Empty;
            dr[colLandings[0]] = le.Landings;
            dr[colNightLandings[0]] = le.NightLandings;
            dr[colFullStopLandings[0]] = le.FullStopLandings;
            dr[colCrossCountry[0]] = le.CrossCountry;
            dr[colNight[0]] = le.Nighttime;
            dr[colIMC[0]] = le.IMC;
            dr[colSimIFR[0]] = le.SimulatedIFR;
            dr[colGroundSim[0]] = le.GroundSim;
            dr[colDual[0]] = le.Dual;
            dr[colCFI[0]] = le.CFI;
            dr[colSIC[0]] = le.SIC;
            dr[colPIC[0]] = le.PIC;
            dr[colRoute[0]] = le.Route;
            dr[colComment[0]] = le.Comment;
            dr[colCatClassOverride[0]] = le.CatClassOverride;
            dr[colEngineStart[0]] = le.EngineStart.FormatDateZulu();
            dr[colEngineEnd[0]] = le.EngineEnd.FormatDateZulu();
            dr[colFlightStart[0]] = le.FlightStart.FormatDateZulu();
            dr[colFlightEnd[0]] = le.FlightEnd.FormatDateZulu();
            dr[colHobbsStart[0]] = le.HobbsStart;
            dr[colHobbsEnd[0]] = le.HobbsEnd;
            dr[colModelName[0]] = le.ModelDisplay;

            if (le.CustomProperties != null)
            {
                foreach (CustomFlightProperty cfp in le.CustomProperties)
                {
                    if (cfp.PropertyType == null)
                        cfp.InitPropertyType(CustomPropertyType.GetCustomPropertyTypes());

                    if (dt.Columns[cfp.PropertyType.Title] == null)
                    {
                        DataColumn dc = new DataColumn(cfp.PropertyType.Title, typeof(string));
                        dt.Columns.Add(dc);
                    }

                    dr[cfp.PropertyType.Title] = cfp.ValueString;
                }
            }
        }

        // Resolve a prop name that could be referenced by alias.
        private static string[] ResolvePropNameAlias(string szPropname)
        {
            string[] retValue;
            if (PropNameAliases.TryGetValue(szPropname, out retValue))
                return retValue;
            else
                return new string[] { szPropname };
        }
        #endregion

        #region Helper classes

        [Serializable]
        private class ImportColumn
        {
            public CustomPropertyType m_cpt;
            public int m_iCol = -1;

            public ImportColumn(CustomPropertyType cpt, int iCol)
            {
                m_cpt = cpt;
                m_iCol = iCol;
            }
        }

        private class RowReader
        {
            private ImportContext m_cm;
            private Dictionary<string, List<Aircraft>> dictFoundAircraft = new Dictionary<string, List<Aircraft>>();
            private string[] m_rgszRow;

            public RowReader(ImportContext columnmapper)
            {
                m_cm = columnmapper;
            }

            public LogbookEntry FlightFromRow(LogbookEntry le, string[] rgszRow)
            {
                if (rgszRow == null)
                    throw new ArgumentNullException("rgszRow");
                m_rgszRow = rgszRow;

                // Check integrity of the row
                if (m_cm.ColumnCount != m_rgszRow.Length)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportIncorrectColumns, m_cm.ColumnCount, m_rgszRow.Length));

                // if it is an existing flight, try loading it; we will modify it from there.  Could generate an exception if the id doesn't exist, or if the user doesn't own it.
                if (m_cm.iColFlightID >= 0)
                {
                    if (!le.FLoadFromDB(GetMappedInt(m_cm.iColFlightID), m_cm.User))
                    {
                        // NotFound is OK - just treat it as a new flight.
                        if (le.LastError != LogbookEntry.ErrorCode.NotFound)
                            throw new MyFlightbookException(le.ErrorString);
                    }
                }
                else
                    le.FLoadFromDB(LogbookEntry.idFlightNew, m_cm.User);

                if (m_cm.iColTail >= m_rgszRow.Length)
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoTail);
                if (m_cm.iColTotal >= m_rgszRow.Length)
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoTotal);
                if (m_cm.iColDate >= m_rgszRow.Length)
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoDate);

                // see if an aircraft ID is present; if so, AND if it matches the specified aircraft, we'll use that
                // (Provides disambiguation if you have two versions of the same aircraft in the account.)
                int idAircraft = (m_cm.iColAircraftID >= 0) ? GetMappedInt(m_cm.iColAircraftID) : Aircraft.idAircraftUnknown;
                if (idAircraft == 0)
                    idAircraft = Aircraft.idAircraftUnknown;

                // check that we know about the aircraft or, if not, if it's in the system then add it for the user.
                string szTail = Aircraft.NormalizeTail(le.TailNumDisplay = m_rgszRow[m_cm.iColTail].Trim().ToUpperInvariant());

                // See if the aircraft exists
                Aircraft ac = null;

                if (String.IsNullOrEmpty(szTail.Trim()))
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportUnknownAircraft, szTail));

                if (m_cm.AircraftForUser.ContainsKey(szTail))
                {
                    if (idAircraft > 0)
                    {
                        UserAircraft ua = new UserAircraft(m_cm.User);
                        Aircraft acByID = ua.GetUserAircraftByID(idAircraft);
                        if (acByID != null && Aircraft.NormalizeTail(acByID.TailNumber).CompareCurrentCultureIgnoreCase(szTail) == 0)   // it matches - use aircraft ID for disambiguation
                            ac = acByID;
                    }
                }
                else
                {
                    if (!dictFoundAircraft.ContainsKey(szTail)) // Avoid more than one DB hit per aircraft
                        dictFoundAircraft[szTail] = Aircraft.AircraftMatchingTail(szTail);
                    List<Aircraft> lst = dictFoundAircraft[szTail];
                    if (lst.Count == 1) // it exists and there are no alternative versions (i.e., no ambiguity) - just go ahead and add it.
                    {
                        m_cm.AircraftForUser[szTail] = lst[0];
                        new UserAircraft(m_cm.User).FAddAircraftForUser(lst[0]);
                    }
                    else
                    {
                        /* 
                         * Aircraft not found - 3 scenarios
                         *  a) No model column or no model specified - just throw a "no aircraft found" exception.
                         *  b) Model column specified - add it to the list of aircraft to import.  Still throw the "no aircraft found" exception, because we need to resolve this before import
                         *  c) anonymous or sim prefix - look it up in the user's profile and match to that if found, and then continue.
                         */
                        bool fFoundAnonOrSim = false;
                        string szModel = string.Empty;

                        if (m_cm.iColModel > 0 && !String.IsNullOrEmpty(szModel = m_rgszRow[m_cm.iColModel]))
                        {
                            // trim anything after a comma, if necessary
                            int i = szModel.IndexOf(",");
                            if (i > 0)
                                szModel = szModel.Substring(0, i);

                            if (CountryCodePrefix.IsNakedSim(szTail) || CountryCodePrefix.IsNakedAnon(szTail))
                            {
                                string szModelNormal = AircraftImportMatchRow.NormalizeModel(szModel);
                                foreach (string szExistingTail in m_cm.AircraftForUser.Keys)
                                {
                                    if (szExistingTail.StartsWith(szTail, StringComparison.CurrentCultureIgnoreCase) &&
                                        AircraftImportMatchRow.NormalizeModel(MakeModel.GetModel(m_cm.AircraftForUser[szExistingTail].ModelID).Model).StartsWith(szModelNormal, StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        fFoundAnonOrSim = true;     // don't throw an exception
                                        szTail = szExistingTail;    // Map to this aircraft
                                        break;
                                    }
                                }
                            }

                            if (!fFoundAnonOrSim)
                                m_cm.AircraftToImport.AddMatchCandidate(szTail, szModel);
                        }
                        else
                            m_cm.AircraftToImport.AddMatchCandidate(szTail, szModel, false);

                        if (!fFoundAnonOrSim)
                            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportUnknownAircraft, szTail));
                    }
                }

                if (ac == null)
                    ac = m_cm.AircraftForUser[szTail];
                le.AircraftID = ac.AircraftID;
                le.TailNumDisplay = ac.DisplayTailnumber;

                if (m_rgszRow[m_cm.iColDate].Length > 0)
                {
                    try
                    {
                        le.Date = Convert.ToDateTime(m_rgszRow[m_cm.iColDate], CultureInfo.CurrentCulture);
                    }
                    catch
                    {
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportCannotReadDate, m_rgszRow[m_cm.iColDate]));
                    }
                }

                le.TotalFlightTime = GetMappedDecimal(m_cm.iColTotal);
                if (le.TotalFlightTime < 0.0M)
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportCannotReadTotalTime, le.TotalFlightTime));

                // Get the other fields, if present.
                le.Approaches = GetMappedInt(m_cm.iColApproaches);
                le.fHoldingProcedures = GetMappedBoolean(m_cm.iColHold);
                le.Landings = GetMappedInt(m_cm.iColLandings);
                le.NightLandings = GetMappedInt(m_cm.iColNightLandings);
                le.FullStopLandings = GetMappedInt(m_cm.iColFullStopLandings);
                le.CrossCountry = GetMappedDecimal(m_cm.iColCrossCountry);
                le.Nighttime = GetMappedDecimal(m_cm.iColNight);
                le.IMC = GetMappedDecimal(m_cm.iColIMC);
                le.SimulatedIFR = GetMappedDecimal(m_cm.iColSimIFR);
                le.GroundSim = GetMappedDecimal(m_cm.iColGroundSim);
                le.Dual = GetMappedDecimal(m_cm.iColDual);
                le.CFI = GetMappedDecimal(m_cm.iColCFI);
                le.SIC = GetMappedDecimal(m_cm.iColSIC);
                le.PIC = GetMappedDecimal(m_cm.iColPIC);

                string szRoute = GetMappedString(m_cm.iColRoute);
                string szFrom = GetMappedString(m_cm.iColFrom);
                string szTo = GetMappedString(m_cm.iColTo);

                // Route is concatenation of From + (Route/Via) + To, if From/To fields are present AND if not redundant.
                if (!String.IsNullOrEmpty(szFrom) && !szRoute.StartsWith(szFrom, StringComparison.CurrentCultureIgnoreCase))
                    szRoute = szFrom + " " + szRoute;
                if (!String.IsNullOrEmpty(szTo) && !szRoute.StartsWith(szTo, StringComparison.CurrentCultureIgnoreCase))
                    szRoute = szRoute + " " + szTo;
                le.Route = szRoute.Trim();

                le.Comment = GetMappedString(m_cm.iColComment);

                le.EngineStart = GetMappedUTCDate(m_cm.iColEngineStart);
                le.EngineEnd = GetMappedUTCDate(m_cm.iColEngineEnd);
                le.FlightStart = GetMappedUTCDate(m_cm.iColFlightStart);
                le.FlightEnd = GetMappedUTCDate(m_cm.iColFlightEnd);
                le.HobbsStart = GetMappedDecimal(m_cm.iColHobbsStart);
                le.HobbsEnd = GetMappedDecimal(m_cm.iColHobbsEnd);

                ArrayList alCustPropsForFlight = new ArrayList();
                foreach (ImportColumn ic in m_cm.CustomPropertiesToImport)
                {
                    string szVal = m_rgszRow[ic.m_iCol];
                    if (szVal.Length > 0)
                    {
                        try
                        {
                            // Re-use the existing property if possible.
                            CustomFlightProperty cfp = (le.CustomProperties == null) ? null : le.CustomProperties.FirstOrDefault(cfpExisting => cfpExisting.PropTypeID == ic.m_cpt.PropTypeID) ?? new CustomFlightProperty(ic.m_cpt);

                            cfp.InitFromString(szVal);
                            if (!cfp.IsDefaultValue)
                                alCustPropsForFlight.Add(cfp);
                        }
                        catch
                        {
                            throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportCannotImportProperty, ic.m_cpt.Title, szVal));
                        }
                    }
                }

                // we now have, from above, a set of custom properties to import.
                // BUT...if this is an existing flight, then some of those flights may already
                // have props.  If so, we want to hold on to those props so that we delete them
                // when we add these.
                // BUT...since we commit the flight before updating the new properties, we don't
                // want to delete properties that have had new values assigned.  SO, those are NOT considered orphans
                if (!le.IsNewFlight)
                {
                    List<CustomFlightProperty> lstExisting = new List<CustomFlightProperty>(le.CustomProperties);
                    lstExisting.RemoveAll(cfp => alCustPropsForFlight.Contains(cfp));   // remove any flight props that are in the ones to import - these are updates, not orphans.
                    m_cm.OrphanedPropsByFlightID[le.FlightID] = lstExisting;
                }

                le.CustomProperties = (CustomFlightProperty[])alCustPropsForFlight.ToArray(typeof(CustomFlightProperty));

                if (!le.IsValid())
                    throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportFlightIsInvalid, le.ErrorString));

                return le;
            }

            #region GetMappedValues
            private Boolean GetMappedBoolean(int iCol)
            {
                if (iCol < 0)
                    return false;
                string sz = m_rgszRow[iCol].ToUpperInvariant().Trim();
                if (sz.Length == 0)
                    return false;
                else
                {
                    if (sz[0] == 'Y' || sz[0] == 'T' || sz[0] == '1')
                        return true;
                    Boolean f = false;
                    if (Boolean.TryParse(sz, out f))
                        return f;
                    return false;
                }
            }

            private string GetMappedString(int iCol)
            {
                return (iCol < 0) ? string.Empty : m_rgszRow[iCol];
            }

            private DateTime GetMappedUTCDate(int iCol)
            {
                DateTime d = DateTime.MinValue;

                if (iCol < 0)
                    return DateTime.MinValue;

                string sz = m_rgszRow[iCol];

                if (DateTime.TryParse(sz, CultureInfo.CurrentCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out d))
                    return d;

                return DateTime.MinValue;
            }

            private Decimal GetMappedDecimal(int iCol)
            {
                if (iCol < 0)
                    return 0.0M;

                string sz = m_rgszRow[iCol].ToUpperInvariant().Trim();

                if (sz.Length == 0)
                    return 0.0M;

                return sz.SafeParseDecimal();
            }

            private Int32 GetMappedInt(int iCol)
            {
                if (iCol < 0)
                    return 0;

                string sz = m_rgszRow[iCol].ToUpperInvariant().Trim();

                Int32 i = 0;
                if (Int32.TryParse(sz, NumberStyles.Any, CultureInfo.CurrentCulture, out i))
                    return i;

                return 0;
            }
            #endregion
        }

        [Serializable]
        private class ImportContext
        {
            #region Column Indices for known columns
            public int iColFlightID { get; set; }
            public int iColDate { get; set; }
            public int iColTail { get; set; }
            public int iColAircraftID { get; set; }
            public int iColTotal { get; set; }
            public int iColApproaches { get; set; }
            public int iColHold { get; set; }
            public int iColLandings { get; set; }
            public int iColNightLandings { get; set; }
            public int iColFullStopLandings { get; set; }
            public int iColCrossCountry { get; set; }
            public int iColNight { get; set; }
            public int iColIMC { get; set; }
            public int iColSimIFR { get; set; }
            public int iColGroundSim { get; set; }
            public int iColDual { get; set; }
            public int iColCFI { get; set; }
            public int iColSIC { get; set; }
            public int iColPIC { get; set; }
            public int iColRoute { get; set; }
            public int iColComment { get; set; }
            public int iColCatClassOverride { get; set; }
            public int iColEngineStart { get; set; }
            public int iColEngineEnd { get; set; }
            public int iColFlightStart { get; set; }
            public int iColFlightEnd { get; set; }
            public int iColHobbsStart { get; set; }
            public int iColHobbsEnd { get; set; }
            public int iColModel { get; set; }
            public int iColFrom { get; set; }
            public int iColTo { get; set; }
            #endregion

            #region public properties
            private Dictionary<string, Aircraft> _dictAircraft;
            public Dictionary<string, Aircraft> AircraftForUser
            {
                get { return _dictAircraft; }
            }

            public string User { get; set; }

            /// <summary>
            /// A dictionary of lists of custom flight properties, indexed by flightID, that represent properties to delete
            /// </summary>
            public Dictionary<int, List<CustomFlightProperty>> OrphanedPropsByFlightID { get; set; }

            public List<ImportColumn> CustomPropertiesToImport { get; set; }
            #endregion

            private Hashtable m_htHeader = new Hashtable();

            /// <summary>
            /// Find the index of a column, given a prioritized array of column headers.
            /// </summary>
            /// <param name="rgsz">Array of column headers</param>
            /// <returns>The index of the first column header found in the file</returns>
            private int ColumnIndex(string[] rgsz)
            {
                for (int i = 0; i < rgsz.Length; i++)
                {
                    object o = m_htHeader[rgsz[i].ToUpperInvariant()];
                    if (o != null)
                        return (int)o;
                }
                return -1;
            }

            private Dictionary<string, Aircraft> DictAircraftForUser(string szUser)
            {
                Dictionary<string, Aircraft> dictReturn = new Dictionary<string, Aircraft>();

                UserAircraft ua = new UserAircraft(szUser);
                Aircraft[] rgac = ua.GetAircraftForUser();

                if (rgac != null)
                {
                    foreach (Aircraft ac in rgac)
                        dictReturn[Aircraft.NormalizeTail(ac.TailNumber)] = ac;
                }

                return dictReturn;
            }

            public AircraftImportParseContext AircraftToImport { get; set; }

            public int ColumnCount
            {
                get { return m_htHeader.Count; }
            }

            private void InitializeColumns()
            {
                iColDate = ColumnIndex(colDate);
                iColTail = ColumnIndex(colTail);
                iColTotal = ColumnIndex(colTotal);

                // verify that required fields are present
                if (iColDate < 0)
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoDate);
                if (iColTail < 0)
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoTail);
                if (iColTotal < 0)
                    throw new MyFlightbookException(Resources.LogbookEntry.errImportNoTotal);

                iColAircraftID = ColumnIndex(colAircraftID);
                iColFlightID = ColumnIndex(colFlightID);
                iColApproaches = ColumnIndex(colApproaches);
                iColHold = ColumnIndex(colHold);
                iColLandings = ColumnIndex(colLandings);
                iColNightLandings = ColumnIndex(colNightLandings);
                iColFullStopLandings = ColumnIndex(colFullStopLandings);
                iColCrossCountry = ColumnIndex(colCrossCountry);
                iColNight = ColumnIndex(colNight);
                iColIMC = ColumnIndex(colIMC);
                iColSimIFR = ColumnIndex(colSimIFR);
                iColGroundSim = ColumnIndex(colGroundSim);
                iColDual = ColumnIndex(colDual);
                iColCFI = ColumnIndex(colCFI);
                iColSIC = ColumnIndex(colSIC);
                iColPIC = ColumnIndex(colPIC);
                iColRoute = ColumnIndex(colRoute);
                iColComment = ColumnIndex(colComment);
                iColCatClassOverride = ColumnIndex(colCatClassOverride);
                iColEngineStart = ColumnIndex(colEngineStart);
                iColEngineEnd = ColumnIndex(colEngineEnd);
                iColFlightStart = ColumnIndex(colFlightStart);
                iColFlightEnd = ColumnIndex(colFlightEnd);
                iColHobbsStart = ColumnIndex(colHobbsStart);
                iColHobbsEnd = ColumnIndex(colHobbsEnd);
                iColModel = ColumnIndex(colModelName);
                iColFrom = ColumnIndex(colFrom);
                iColTo = ColumnIndex(colTo);

                // Now, see which custom properties are present
                CustomPropertyType[] rgCpt = CustomPropertyType.GetCustomPropertyTypes();
                foreach (CustomPropertyType cpt in rgCpt)
                {
                    int iCol = ColumnIndex(ResolvePropNameAlias(cpt.Title));
                    if (iCol >= 0)
                        CustomPropertiesToImport.Add(new ImportColumn(cpt, iCol));
                }
            }

            public ImportContext(string[] rgszHeader, string szUser)
            {
                User = szUser;
                _dictAircraft = DictAircraftForUser(szUser);
                AircraftToImport = new AircraftImportParseContext();

                OrphanedPropsByFlightID = new Dictionary<int, List<CustomFlightProperty>>();
                CustomPropertiesToImport = new List<ImportColumn>();

                for (int i = 0; i < rgszHeader.Length; i++)
                {
                    if (String.IsNullOrWhiteSpace(rgszHeader[i]))
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportEmptyColumn, i + 1));
                    string szNormal = rgszHeader[i].ToUpperInvariant().Trim();
                    if (m_htHeader[szNormal] != null)
                        throw new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, Resources.LogbookEntry.errImportDuplicateColumn, szNormal));
                    m_htHeader[szNormal] = i;    // set up to find each column by name
                }

                InitializeColumns();
            }
        }
        #endregion

        public CSVImporter()
        {
        }

        /// <summary>
        /// Reads a CSV file and returns a list of LogbookEntry objects from it. DOES NOT WRITE THE FLIGHTS!!!
        /// </summary>
        /// <param name="fileContent">The CSV file stream</param>
        /// <param name="szUser">The username for whom the import is being performed</param>
        /// <param name="rowHasError">Delegate called for a row that has an error.  Has the entry (error string indicates the error), the raw row data, and the index of the row</param>
        /// <param name="rowOK">Delegate called for a row that does not have an error.  Has the entry and the row index</param>
        /// <returns>false for an error (look at "ErrorString" for information).</returns>
        public bool FInitFromStream(Stream fileContent, string szUser, Action<LogbookEntry, int> rowOK, Action<LogbookEntry, string, int> rowHasError)
        {
            using (CSVReader csvr = new CSVReader(fileContent))
            {
                FlightsToImport = new List<LogbookEntry>();
                int iRow = 0;

                bool fUseHHMM = Profile.GetUser(szUser).UsesHHMM;
                HasErrors = false;
                ErrorMessage = string.Empty;

                try
                {
                    try
                    {
                        m_ImportContext = new ImportContext(csvr.GetCSVLine(true), szUser);
                    }
                    catch (CSVReaderInvalidCSVException ex)
                    {
                        throw new MyFlightbookException(ex.Message);
                    }

                    RowReader rr = new RowReader(m_ImportContext);

                    string[] rgszRow = null;
                    while ((rgszRow = csvr.GetCSVLine()) != null)
                    {
                        iRow++;

                        // Check for empty row; skip it if necessary
                        bool fHasData = false;
                        Array.ForEach<string>(rgszRow, (sz) => { if (sz.Trim().Length > 0) fHasData = true; });
                        if (!fHasData)
                            continue;

                        LogbookEntry le = new LogbookEntry();

                        try
                        {
                            le = rr.FlightFromRow(le, rgszRow);
                            if (rowOK != null)
                                rowOK(le, iRow);
                        }
                        catch (MyFlightbookException ex)
                        {
                            HasErrors = true;
                            le.ErrorString = ex.Message;
                            if (rowHasError != null)
                                rowHasError(le, String.Join(",", rgszRow), iRow);
                        }

                        FlightsToImport.Add(le);
                    }
                    m_ImportContext.AircraftToImport.ProcessParseResultsForUser(szUser);
                    m_missingAircraft.AddRange(m_ImportContext.AircraftToImport.AllMissing);
                }
                catch (MyFlightbookException ex)
                {
                    HasErrors = true;
                    ErrorMessage = ex.Message;
                    FlightsToImport = null;
                    return false;
                }
            }

            return !HasErrors; ;
        }

        /// <summary>
        /// Commits the initialized flights
        /// </summary>
        /// <param name="OnRowAdded">Delegate called for each flight that is commited</param>
        /// <returns>True for success</returns>
        public bool FCommit(Action<LogbookEntry, bool> OnRowAdded, Action<LogbookEntry, Exception> OnRowFailure)
        {
            if (FlightsToImport == null || HasErrors)
                return false;

            int cFlightsImported = 0;

            // Write the data to the db.
            foreach (LogbookEntry le in FlightsToImport)
            {
                le.ErrorString = string.Empty;
                List<CustomFlightProperty> lstCFPToDelete = le.IsNewFlight ? null : m_ImportContext.OrphanedPropsByFlightID[le.FlightID];
                try
                {
                    bool fInsert = le.IsNewFlight;
                    if (le.FCommit() && lstCFPToDelete != null)
                    {
                        foreach (CustomFlightProperty cfp in lstCFPToDelete)
                            cfp.DeleteProperty();
                    }

                    if (OnRowAdded != null)
                        OnRowAdded(le, fInsert);

                    cFlightsImported++;
                }
                catch (MyFlightbookException ex)
                {
                    le.ErrorString = ex.Message;
                    if (OnRowFailure != null)
                        OnRowFailure(le, ex);
                }
            }

            EventRecorder.UpdateCount(EventRecorder.MFBCountID.ImportedFlight, cFlightsImported);
            return true;
        }
    }
}