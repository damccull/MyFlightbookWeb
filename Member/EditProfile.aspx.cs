using MyFlightbook;
using MyFlightbook.Basicmed;
using MyFlightbook.CloudStorage;
using MyFlightbook.FlightCurrency;
using MyFlightbook.Payments;
using MyFlightbook.SocialMedia;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;

/******************************************************
 * 
 * Copyright (c) 2010-2018 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

public partial class Member_EditProfile : System.Web.UI.Page
{
    private Profile m_pf = null;
    private const string szParamDropboxAuth = "dbOAuth";
    private const string szParamGDriveAuth = "gdOAuth";
    private const string szParam1DriveAuth = "1dOAuth";

    protected override void OnError(EventArgs e)
    {
        Exception ex = Server.GetLastError();

        if (ex.GetType() == typeof(HttpRequestValidationException))
        {
            Context.ClearError();
            Response.Redirect("~/SecurityError.aspx");
            Response.End();
        }
        else
            base.OnError(e);
    }

    #region Page Lifecycle, setup
    private tabID SetUpSidebar()
    {
        tabID sidebarTab = tabID.tabUnknown;

        string szPrefPath = String.IsNullOrWhiteSpace(Request.PathInfo) ? string.Empty : Request.PathInfo.Substring(1);
        string[] rgPrefPath = szPrefPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        string szPref = util.GetStringParam(Request, "pref");

        if (rgPrefPath.Length > 0 && !String.IsNullOrEmpty(rgPrefPath[0]))
        {
            try { sidebarTab = (tabID)Enum.Parse(typeof(tabID), rgPrefPath[0]); }
            catch (ArgumentException) { }
            catch (OverflowException) { }
        }

        if (sidebarTab == tabID.tabUnknown && !String.IsNullOrEmpty(szPref))
        {
            // Backwards compatibility - redirect if using the szPref method 
            try
            {
                sidebarTab = (tabID)Enum.Parse(typeof(tabID), szPref);

                // Redirect now using PathInfo scheme
                string szNew = Request.Url.PathAndQuery.Replace(".aspx", String.Format(CultureInfo.InvariantCulture, ".aspx/{0}", szPref)).Replace(String.Format(CultureInfo.InvariantCulture, "pref={0}", szPref), string.Empty).Replace("?&", "?");
                Response.Redirect(szNew, true);
                return sidebarTab;
            }
            catch (ArgumentException) { }
            catch (OverflowException) { }
        }

        if (sidebarTab == tabID.tabUnknown)
        {
            // have a less fragile way of linking.
            switch (szPref)
            {
                default:
                case "name":
                    sidebarTab = tabID.pftName;
                    break;
                case "pass":
                    sidebarTab = tabID.pftPass;
                    break;
                case "qa":
                    sidebarTab = tabID.pftQA;
                    break;
                case "pref":
                case "email":
                    sidebarTab = tabID.pftPrefs;
                    break;
                case "pilotinfo":
                    sidebarTab = tabID.pftPilotInfo;
                    break;
                case "instruction":
                    sidebarTab = tabID.instEndorsements;
                    break;
            }
        }

        this.Master.SelectedTab = sidebarTab;

        switch (sidebarTab)
        {
            case tabID.pftAccount:
            case tabID.pftName:
            case tabID.pftPass:
            case tabID.pftQA:
                mvProfile.SetActiveView(vwAccount);
                this.Master.SelectedTab = tabID.pftAccount;
                break;
            case tabID.pftPrefs:
                mvProfile.SetActiveView(vwPrefs);
                break;
            case tabID.pftPilotInfo:
                mvProfile.SetActiveView(vwPilotInfo);
                break;
            // Backwards compatibility: we moved instructors/students/endorsements/signing to InstStudent, but may still be requested here.
            // Redirect for any of those
            case tabID.instEndorsements:
            case tabID.instSignFlights:
            case tabID.instStudents:
            case tabID.instInstructors:
                Response.Redirect(Request.Url.PathAndQuery.Replace("EditProfile.aspx", String.Format(CultureInfo.InvariantCulture, "Training.aspx/{0}", szPref)).Replace(String.Format(CultureInfo.InvariantCulture, "pref={0}", szPref), string.Empty).Replace("?&", "?"));
                break;
            case tabID.pftDonate:
                mvProfile.SetActiveView(vwDonate);
                break;
        }

        return sidebarTab;
    }

    private void InitAccount(tabID sidebarTab)
    {
        txtFirst.Text = m_pf.FirstName;
        txtLast.Text = m_pf.LastName;
        txtEmail2.Text = txtEmail.Text = m_pf.Email;
        lblQuestion.Text = m_pf.SecurityQuestion;
        txtAddress.Text = m_pf.Address;
        accordianAccount.SelectedIndex = (sidebarTab == tabID.pftQA) ? 2 : (sidebarTab == tabID.pftPass ? 1 : 0);
    }

    private void InitPilotInfo()
    {
        dateMedical.Date = m_pf.LastMedical;
        cmbMonthsMedical.SelectedValue = m_pf.MonthsToMedical.ToString(CultureInfo.CurrentCulture);
        rblMedicalDurationType.SelectedIndex = m_pf.UsesICAOMedical ? 1 : 0;
        txtCertificate.Text = m_pf.Certificate;
        txtLicense.Text = m_pf.License;
        mfbTypeInDateCFIExpiration.Date = m_pf.CertificateExpiration;
        mfbDateEnglishCheck.Date = m_pf.EnglishProficiencyExpiration;
        UpdateNextMedical(m_pf);
        BasicMedManager.RefreshBasicMedEvents();

        gvIPC.DataSource = ProfileEvent.GetIPCEvents(User.Identity.Name);
        gvIPC.DataBind();

        MyFlightbook.Achievements.UserRatings ur = new MyFlightbook.Achievements.UserRatings(m_pf.UserName);
        gvRatings.DataSource = ur.Licenses;
        gvRatings.DataBind();

        ProfileEvent[] rgpfeBFR = ProfileEvent.GetBFREvents(User.Identity.Name, m_pf.LastBFREvent);

        gvBFR.DataSource = rgpfeBFR;
        gvBFR.DataBind();

        if (rgpfeBFR.Length > 0) // we have at least one BFR event, so the last one should be the most recent.
        {
            lblNextBFR.Text = m_pf.NextBFR(rgpfeBFR[rgpfeBFR.Length - 1].Date).ToShortDateString();
            pnlNextBFR.Visible = true;
        }

        string szPane = Request["pane"];
        if (!String.IsNullOrEmpty(szPane))
        {
            for (int i = 0; i < accordianPilotInfo.Panes.Count; i++)
            {
                switch (szPane)
                {
                    case "medical":
                        if (accordianPilotInfo.Panes[i] == acpMedical)
                            accordianPilotInfo.SelectedIndex = i;
                        break;
                    case "certificates":
                        if (accordianPilotInfo.Panes[i] == acpCertificates)
                            accordianPilotInfo.SelectedIndex = i;
                        break;
                    case "flightreview":
                        if (accordianPilotInfo.Panes[i] == acpFlightReviews)
                            accordianPilotInfo.SelectedIndex = i;
                        break;
                    case "ipc":
                        if (accordianPilotInfo.Panes[i] == acpIPCs)
                            accordianPilotInfo.SelectedIndex = i;
                        break;
                    default:
                        break;
                }
            }
        }
    }

    private void InitFeaturePrefs()
    {
        ckShowTimes.Checked = m_pf.DisplayTimesByDefault;
        ckSIC.Checked = m_pf.TracksSecondInCommandTime;
        ckTrackCFITime.Checked = m_pf.IsInstructor;
        ckUseArmyCurrency.Checked = m_pf.UsesArmyCurrency;
        ckUse117DutyTime.Checked = m_pf.UsesFAR117DutyTime;
        rbl117Rules.SelectedIndex = m_pf.UsesFAR117DutyTimeAllFlights ? 1 : 0;
        ckUse135DutyTime.Checked = m_pf.UsesFAR135DutyTime;
        ckUse13529xCurrency.Checked = m_pf.UsesFAR13529xCurrency;
        ckUse13526xCurrency.Checked = m_pf.UsesFAR13526xCurrency;
        ckUse61217Currency.Checked = m_pf.UsesFAR61217Currency;
        ckLAPLCurrency.Checked = m_pf.UsesLAPLCurrency;
        ck6157c4Pref.Checked = m_pf.UsesLooseIFRCurrency;
        pnlLoose6157.Visible = DateTime.Now.CompareTo(ExaminerFlightRow.Nov2018Cutover) < 0;
        ckCanadianCurrency.Checked = m_pf.UseCanadianCurrencyRules;
        rblTotalsOptions.SelectedValue = m_pf.TotalsGroupingMode.ToString();
        rblCurrencyPref.SelectedIndex = (m_pf.UsesPerModelCurrency ? 1 : 0);
        rblTimeEntryPreference.SelectedIndex = (m_pf.UsesHHMM ? 1 : 0);
        rblDateEntryPreferences.SelectedIndex = (m_pf.UsesUTCDateOfFlight ? 1 : 0);
        foreach (CurrencyExpiration.Expiration exp in Enum.GetValues(typeof(CurrencyExpiration.Expiration)))
        {
            ListItem li = new ListItem(CurrencyExpiration.ExpirationLabel(exp), exp.ToString());
            li.Selected = m_pf.CurrencyExpiration == exp;
            cmbExpiredCurrency.Items.Add(li);
        }
    }

    private void InitSocialNetworking()
    {
        lblCanTweet.Text = Branding.ReBrand(Resources.Profile.TwitterIsAuthed);
        lnkNoTweet.Text = Branding.ReBrand(Resources.Profile.DeAuthTwitter);
        lnkSetUpTwitter.Text = Branding.ReBrand(Resources.Profile.AuthorizeTwitter);
        locFBIsAuthorized.Text = Branding.ReBrand(Resources.Profile.FacebookIsAuthed);
        lnkNoFacebook.Text = Branding.ReBrand(Resources.Profile.DeAuthFacebook);
        lnkSetUpFacebook.Text = Branding.ReBrand(Resources.Profile.AuthorizeFacebook);


        ShowTweetState(m_pf.CanTweet());
        imgMyFlightbook.ImageUrl = Branding.CurrentBrand.LogoURL;
        imgMyFlightbook.ToolTip = imgMyFlightbook.AlternateText = Branding.CurrentBrand.AppName;

        ShowFacebookState(m_pf.CanPostFacebook());
        ShowGooglePlusState(m_pf.CanPostGooglePlus());

        // Sharing
        lnkMyFlights.Text = lnkMyFlights.NavigateUrl = m_pf.PublicFlightsURL(Request.Url.Host).AbsoluteUri;
    }

    private void InitCloudProviders()
    {
        List<StorageID> lstCloud = new List<StorageID>(m_pf.AvailableCloudProviders);
        StorageID defaultStorage = m_pf.BestCloudStorage;
        foreach (StorageID sid in lstCloud)
            cmbDefaultCloud.Items.Add(new ListItem(CloudStorageBase.CloudStorageName(sid), sid.ToString()) { Selected = defaultStorage == sid });
        pnlDefaultCloud.Visible = lstCloud.Count > 1;   // only show a choice if more than one cloud provider is set up

        mvDropBoxState.SetActiveView(lstCloud.Contains(StorageID.Dropbox) ? vwDeAuthDropbox : vwAuthDropBox);
        mvGDriveState.SetActiveView(lstCloud.Contains(StorageID.GoogleDrive) ? vwDeAuthGDrive : vwAuthGDrive);
        mvOneDriveState.SetActiveView(lstCloud.Contains(StorageID.OneDrive) ? vwDeAuthOneDrive : vwAuthOneDrive);

        locAboutCloudStorage.Text = Branding.ReBrand(Resources.Profile.AboutCloudStorage);
        lnkAuthDropbox.Text = Branding.ReBrand(Resources.Profile.AuthorizeDropbox);
        lnkDeAuthDropbox.Text = Branding.ReBrand(Resources.Profile.DeAuthDropbox);
        locDropboxIsAuthed.Text = Branding.ReBrand(Resources.Profile.DropboxIsAuthed);
        lnkAuthorizeGDrive.Text = Branding.ReBrand(Resources.Profile.AuthorizeGDrive);
        lnkDeAuthGDrive.Text = Branding.ReBrand(Resources.Profile.DeAuthGDrive);
        locGoogleDriveIsAuthed.Text = Branding.ReBrand(Resources.Profile.GDriveIsAuthed);
        lnkAuthorizeOneDrive.Text = Branding.ReBrand(Resources.Profile.AuthorizeOneDrive);
        lnkDeAuthOneDrive.Text = Branding.ReBrand(Resources.Profile.DeAuthOneDrive);
        locOneDriveIsAuthed.Text = Branding.ReBrand(Resources.Profile.OneDriveIsAuthed);

        rblCloudBackupAppendDate.SelectedValue = m_pf.OverwriteCloudBackup.ToString(CultureInfo.InvariantCulture);
    }

    private void HandleOAuthRedirect()
    {
        if (!String.IsNullOrEmpty(Request.Params[szParamDropboxAuth])) // redirect from Dropbox oAuth request.
        {
            m_pf.DropboxAccessToken = new MFBDropbox().ConvertToken(Request).AccessToken;
            m_pf.FCommit();

            Response.Redirect(String.Format(CultureInfo.InvariantCulture, "{0}?pane=backup", Request.Path));
        }
        if (!String.IsNullOrEmpty(Request.Params[szParamGDriveAuth])) // redirect from GDrive oAuth request.
        {
            m_pf.GoogleDriveAccessToken = new GoogleDrive().ConvertToken(Request);
            m_pf.FCommit();

            Response.Redirect(String.Format(CultureInfo.InvariantCulture, "{0}?pane=backup", Request.Path));
        }
        if (!String.IsNullOrEmpty(Request.Params[szParam1DriveAuth])) // redirect from OneDrive oAuth request.
        {
            m_pf.OneDriveAccessToken = (DotNetOpenAuth.OAuth2.AuthorizationState)Session[OneDrive.TokenSessionKey];
            m_pf.FCommit();

            Response.Redirect(String.Format(CultureInfo.InvariantCulture, "{0}?pane=backup", Request.Path));
        }
        if (!String.IsNullOrEmpty(Request.Params[MFBFacebook.OAuthParam]))   // redirect from FB oAuth request
        {
            try
            {
                m_pf.FacebookAccessToken = MFBFacebook.ConvertToken(Request);
                m_pf.FCommit();
                mfbFacebook1.PostPendingFlight();
            }
            catch (MyFlightbookException) { }

            Response.Redirect(SocialNetworkAuthorization.PopRedirect(String.Format(CultureInfo.InvariantCulture, "{0}?pane=social", Request.Path)));
        }
    }

    private void InitDeadlinesAndCurrencies()
    {
        // Custom Currencies.
        mfbCustomCurrencyList1.RefreshCustomCurrencyList();

        // for deadline aircraft, restrict to real aircraft
        UserAircraft ua = new UserAircraft(Page.User.Identity.Name);
        List<Aircraft> lstDeadlineAircraft = new List<Aircraft>(ua.GetAircraftForUser());
        lstDeadlineAircraft.RemoveAll(ac => ac.InstanceType != AircraftInstanceTypes.RealAircraft);
        lstDeadlineAircraft.RemoveAll(ac => ac.HideFromSelection);
        cmbDeadlineAircraft.DataSource = lstDeadlineAircraft;
        cmbDeadlineAircraft.DataBind();
        decRegenInterval.EditBox.Attributes["onfocus"] = String.Format(CultureInfo.InvariantCulture, "javascript:document.getElementById('{0}').checked = true;", rbRegenInterval.ClientID);

        mfbDeadlines1.UserName = Page.User.Identity.Name;
        mfbDeadlines1.Refresh();
    }

    private void InitPrefs()
    {
        // features
        InitFeaturePrefs();

        // Property blacklist
        UpdateBlacklist();

        // Social networking.
        InitSocialNetworking();

        // Set up status of cloud providers and ability to pick a default.
        InitCloudProviders();

        HandleOAuthRedirect();

        acpoAuthApps.Visible = oAuthAuthorizationManager.Refresh();

        string szPane = Request["pane"];
        if (!String.IsNullOrEmpty(szPane))
        {
            acpLocalPrefs.Visible = String.IsNullOrEmpty(Request["nolocalprefs"]);
            int paneIndexOffset = acpLocalPrefs.Visible ? 0 : -1;

            switch (szPane)
            {
                case "backup":
                    for (int i = 0; i < accordianPrefs.Panes.Count; i++)
                        if (accordianPrefs.Panes[i] == acpBackup)
                        {
                            accordianPrefs.SelectedIndex = i + paneIndexOffset;
                            break;
                        }
                    break;
                case "social":
                    for (int i = 0; i < accordianPrefs.Panes.Count; i++)
                        if (accordianPrefs.Panes[i] == acpSocialNetworking)
                        {
                            accordianPrefs.SelectedIndex = i + paneIndexOffset;
                            break;
                        }
                    break;
                case "deadlines":
                    for (int i = 0; i < accordianPrefs.Panes.Count; i++)
                        if (accordianPrefs.Panes[i] == acpCustomDeadlines)
                        {
                            accordianPrefs.SelectedIndex = i + paneIndexOffset;
                            break;
                        }
                    break;
                case "custcurrency":
                    for (int i = 0; i < accordianPrefs.Panes.Count; i++)
                        if (accordianPrefs.Panes[i] == acpCustomCurrencies)
                        {
                            accordianPrefs.SelectedIndex = i + paneIndexOffset;
                            break;
                        }
                    break;
                default:
                    break;
            }
        }

        InitDeadlinesAndCurrencies();
    }

    private void InitDonations()
    {
        List<Gratuity> lstKnownGratuities = new List<Gratuity>(Gratuity.KnownGratuities);
        lstKnownGratuities.Sort((g1, g2) => { return g1.Threshold.CompareTo(g2.Threshold); });
        rptAvailableGratuities.DataSource = lstKnownGratuities;
        rptAvailableGratuities.DataBind();

        pnlPaypalCanceled.Visible = util.GetStringParam(Request, "pp").CompareCurrentCultureIgnoreCase("canceled") == 0;
        pnlPaypalSuccess.Visible = util.GetStringParam(Request, "pp").CompareCurrentCultureIgnoreCase("success") == 0;
        lblDonatePrompt.Text = Branding.ReBrand(Resources.LocalizedText.DonatePrompt);
        gvDonations.DataSource = Payment.RecordsForUser(User.Identity.Name);
        gvDonations.DataBind();

        List<EarnedGrauity> lst = EarnedGrauity.GratuitiesForUser(User.Identity.Name, Gratuity.GratuityTypes.Unknown);
        lst.RemoveAll(eg => eg.CurrentStatus == EarnedGrauity.EarnedGratuityStatus.Expired);
        if (pnlEarnedGratuities.Visible = (lst.Count > 0))
        {
            rptEarnedGratuities.DataSource = lst;
            rptEarnedGratuities.DataBind();
        }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        m_pf = MyFlightbook.Profile.GetUser(User.Identity.Name);

        if (!Request.IsSecureConnection && !Request.IsLocal)
            Response.Redirect(Request.Url.AbsoluteUri.Replace("http://", "https://"));

        lblName.Text = String.Format(CultureInfo.CurrentCulture, Resources.Profile.EditProfileHeader, m_pf.UserFullName);

        this.Master.Title = String.Format(CultureInfo.CurrentCulture, Resources.LocalizedText.TitleProfile, Branding.CurrentBrand.AppName);

        // sidebar doesn't store it's state, so just set the currenttab each time.
        tabID sidebarTab = SetUpSidebar();

        if (!IsPostBack)
        {
            // Set pilot info validation group
            util.SetValidationGroup(mfbTypeInDateCFIExpiration, btnUpdatePilotInfo.ValidationGroup);
            util.SetValidationGroup(dateMedical, btnUpdatePilotInfo.ValidationGroup);

            // Set up per-section information
            switch (sidebarTab)
            {
                case tabID.pftAccount:
                case tabID.pftName:
                case tabID.pftPass:
                case tabID.pftQA:
                    InitAccount(sidebarTab);
                    break;

                case tabID.pftPilotInfo:
                    InitPilotInfo();
                    break;

                case tabID.pftPrefs:
                    InitPrefs();
                    break;

                case tabID.pftDonate:
                    InitDonations();
                    break;
            }
        }
    }
    #endregion

    #region Name and Email
    protected Boolean FCommitName()
    {
        if (!IsValid)
            return false;

        try
        {
            m_pf.ChangeNameAndEmail(txtFirst.Text, txtLast.Text, txtEmail.Text, txtAddress.Text);
            m_pf.FCommit();
        }
        catch (MyFlightbookException ex)
        {
            lblNameUpdated.Visible = true;
            lblNameUpdated.Text = ex.Message;
            lblNameUpdated.CssClass = "error";
            return false;
        }

        return true;
    }

    protected void btnUpdatename_Click(object sender, EventArgs e)
    {
        if (FCommitName())
            lblNameUpdated.Visible = true;
    }

    public void VerifyEmailAvailable(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
#pragma warning disable 0618
        Profile pf = new Profile(User.Identity.Name);
#pragma warning restore 0618
        args.IsValid = true;
        if (String.Compare(txtEmail.Text, pf.Email, StringComparison.OrdinalIgnoreCase) != 0)
        {
            // see if it's available
            pf.Email = txtEmail.Text;
            args.IsValid = pf.FIsValid();
        }
    }
    #endregion

    #region Password
    protected Boolean FCommitPass()
    {
        try
        {
            if (CurrentPassword.Text.Length == 0 || !Membership.ValidateUser(User.Identity.Name, CurrentPassword.Text))
                throw new MyFlightbookException(Resources.Profile.errBadPasswordToChange);
            if (NewPassword.Text.Length < 6)
                throw new MyFlightbookException(Resources.Profile.errBadPasswordLength);
            if (NewPassword.Text != ConfirmNewPassword.Text) // should never happen - validation should have caught this.
                throw new MyFlightbookException(Resources.Profile.errPasswordsDontMatch);
            if (!Membership.Provider.ChangePassword(Page.User.Identity.Name, CurrentPassword.Text, NewPassword.Text))
                throw new MyFlightbookException(Resources.Profile.errChangePasswordFailed);

            util.NotifyUser(String.Format(CultureInfo.CurrentCulture, Resources.Profile.PasswordChangedSubject, Branding.CurrentBrand.AppName),
                Branding.ReBrand(Resources.EmailTemplates.PasswordChanged),
                new System.Net.Mail.MailAddress(m_pf.Email, m_pf.UserFullName), false, false);
        }
        catch (MyFlightbookException ex)
        {
            lblPassChanged.Visible = true;
            lblPassChanged.Text = ex.Message;
            lblPassChanged.CssClass = "error";
            return false;
        }

        return true;
    }

    protected void btnUpdatePass_Click(object sender, EventArgs e)
    {
        if (FCommitPass())
            lblPassChanged.Visible = true;
    }

    public void ValidateCurrentPassword(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        if (CurrentPassword.Text.Length == 0 && NewPassword.Text.Length > 0)
            args.IsValid = false;
    }
    #endregion

    #region Q and A
    protected void btnChangeQA_Click(object sender, EventArgs e)
    {
        try
        {
            // see if we need to change question too.
            if ((txtQuestion.Text.Length > 0) ^ (txtAnswer.Text.Length > 0))
                throw new MyFlightbookException(Resources.Profile.errNeedBothQandA);

            if (txtQuestion.Text.Length == 0 || txtAnswer.Text.Length == 0) // should have been caught
                throw new MyFlightbookException(Resources.Profile.errPleaseTypeNewQandA);

            if (!Membership.Provider.ValidateUser(Page.User.Identity.Name, txtPassQA.Text))
                throw new MyFlightbookException(Resources.Profile.errIncorrectPassword);

            // change both password and question/answer
            if (!Membership.Provider.ChangePasswordQuestionAndAnswer(Page.User.Identity.Name, txtPassQA.Text, txtQuestion.Text, txtAnswer.Text))
                throw new MyFlightbookException(Resources.Profile.errChangeQandAFailed);

            m_pf.SecurityQuestion = txtQuestion.Text;
            lblQAChangeSuccess.Visible = true;
            m_pf.FCommit(); // update the cache

        }
        catch (MyFlightbookException ex)
        {
            lblQAChangeSuccess.Visible = true;
            lblQAChangeSuccess.Text = ex.Message;
            lblQAChangeSuccess.CssClass = "error";
        }
    }
    #endregion

    #region Preferences
    protected void btnUpdateLocalPrefs_Click(object sender, EventArgs e)
    {
        m_pf.DisplayTimesByDefault = ckShowTimes.Checked;
        m_pf.TracksSecondInCommandTime = ckSIC.Checked;
        m_pf.IsInstructor = ckTrackCFITime.Checked;
        m_pf.UsesHHMM = (rblTimeEntryPreference.SelectedIndex > 0);
        m_pf.UsesUTCDateOfFlight = (rblDateEntryPreferences.SelectedIndex > 0);

        try
        {
            m_pf.FCommit();
            lblLocalPrefsUpdated.Visible = true;
        }
        catch (MyFlightbookException ex)
        {
            lblLocalPrefsUpdated.Visible = true;
            lblLocalPrefsUpdated.Text = ex.Message;
            lblLocalPrefsUpdated.CssClass = "error";
        }
    }

    protected void btnUpdateCurrencyPrefs_Click(object sender, EventArgs e)
    {
        m_pf.UsesArmyCurrency = ckUseArmyCurrency.Checked;
        m_pf.UsesFAR117DutyTime = ckUse117DutyTime.Checked;
        m_pf.UsesFAR117DutyTimeAllFlights = rbl117Rules.SelectedIndex != 0;
        m_pf.UsesFAR135DutyTime = ckUse135DutyTime.Checked;
        m_pf.UsesFAR13529xCurrency = ckUse13529xCurrency.Checked;
        m_pf.UsesFAR13526xCurrency = ckUse13526xCurrency.Checked;
        m_pf.UsesFAR61217Currency = ckUse61217Currency.Checked;
        m_pf.UsesLooseIFRCurrency = ck6157c4Pref.Checked;
        m_pf.UseCanadianCurrencyRules = ckCanadianCurrency.Checked;
        m_pf.UsesLAPLCurrency = ckLAPLCurrency.Checked;
        m_pf.UsesPerModelCurrency = (rblCurrencyPref.SelectedIndex > 0);
        m_pf.TotalsGroupingMode = (TotalsGrouping) Enum.Parse(typeof(TotalsGrouping), rblTotalsOptions.SelectedValue);
        m_pf.CurrencyExpiration = (CurrencyExpiration.Expiration)Enum.Parse(typeof(CurrencyExpiration.Expiration), cmbExpiredCurrency.SelectedValue);

        try
        {
            m_pf.FCommit();
            lblCurrencyPrefsUpdated.Visible = true;
        }
        catch (MyFlightbookException ex)
        {
            lblCurrencyPrefsUpdated.Visible = true;
            lblCurrencyPrefsUpdated.Text = ex.Message;
            lblCurrencyPrefsUpdated.CssClass = "error";
        }
    }
    #region Deadlines
    protected void SetNewDeadlineMode(bool fHours)
    {
        mvDeadlineDue.SetActiveView(fHours ? vwDeadlineDueHours : vwDeadlineDueDate);
        mvRegenInterval.SetActiveView(fHours ? vwDeadlineHours : vwDeadlineCalendarRange);
        if (fHours)
            mfbDeadlineDate.Date = DateTime.MinValue;
        else
            decDueHours.Value = 0.0M;
    }

    protected void ckDeadlineUseHours_CheckedChanged(object sender, EventArgs e)
    {
        SetNewDeadlineMode(ckDeadlineUseHours.Checked);
    }

    protected void cmbDeadlineAircraft_SelectedIndexChanged(object sender, EventArgs e)
    {
        ckDeadlineUseHours.Visible = !String.IsNullOrEmpty(cmbDeadlineAircraft.SelectedValue);
        if (!ckDeadlineUseHours.Visible)
            ckDeadlineUseHours.Checked = false;
        SetNewDeadlineMode(ckDeadlineUseHours.Checked);
    }

    protected void btnAddDeadline_Click(object sender, EventArgs e)
    {
        int regenspan;
        DeadlineCurrency.RegenUnit ru = rbRegenManual.Checked ? DeadlineCurrency.RegenUnit.None : (ckDeadlineUseHours.Checked ? DeadlineCurrency.RegenUnit.Hours : (DeadlineCurrency.RegenUnit)Enum.Parse(typeof(DeadlineCurrency.RegenUnit), cmbRegenRange.SelectedValue));
        switch (ru)
        {
            default:
            case DeadlineCurrency.RegenUnit.None:
                regenspan = 0;
                break;
            case DeadlineCurrency.RegenUnit.Days:
            case DeadlineCurrency.RegenUnit.CalendarMonths:
            case DeadlineCurrency.RegenUnit.Hours:
                regenspan = decRegenInterval.IntValue;
                break;
        }

        decimal aircraftHours = decDueHours.Value;
        int idAircraft = 0;
        if (!String.IsNullOrEmpty(cmbDeadlineAircraft.SelectedValue))
            idAircraft = Convert.ToInt32(cmbDeadlineAircraft.SelectedValue, CultureInfo.InvariantCulture);

        DeadlineCurrency dc = new DeadlineCurrency(Page.User.Identity.Name, txtDeadlineName.Text, mfbDeadlineDate.Date, regenspan, ru, idAircraft, aircraftHours);
        if (dc.IsValid() && dc.FCommit())
        {
            mfbDeadlines1.ForceRefresh();
            ResetDeadlineForm();
        }
        else
            lblErrDeadline.Text = dc.ErrorString;
    }

    protected void ResetDeadlineForm()
    {
        ckDeadlineUseHours.Checked = false;
        cmbDeadlineAircraft.SelectedValue = string.Empty;
        SetNewDeadlineMode(false);
        txtDeadlineName.Text = string.Empty;
        mfbDeadlineDate.Date = DateTime.MinValue;
        decDueHours.Value = decRegenInterval.IntValue = 0;
        ckDeadlineUseHours.Visible = false;
        rbRegenManual.Checked = true;
        cpeDeadlines.ClientState = "true";
    }
    #endregion

    #region Social Networking
    protected void ShowTweetState(Boolean fCanTweet)
    {
        lnkSetUpTwitter.Visible = !fCanTweet;
        pnlCanTweet.Visible = fCanTweet;
    }

    protected void ShowFacebookState(Boolean fCanPost)
    {
        lnkSetUpFacebook.Visible = !fCanPost;
        pnlFacebookAuthorized.Visible = fCanPost;
    }

    protected void ShowGooglePlusState(Boolean fCanPost)
    {
        lnkAuthorizeGooglePlus.Visible = !fCanPost;
        pnlGooglePlus.Visible = fCanPost;
    }

    protected void lnkNoTweet_Click(object sender, EventArgs e)
    {
        m_pf.TwitterAccessSecret = m_pf.TwitterAccessToken = "";
        m_pf.FCommit(); // should be no need to validate profile object here - I'm just changing twitter settings.
        ShowTweetState(m_pf.CanTweet());
    }

    protected void lnkNoFacebook_Click(object sender, EventArgs e)
    {
        m_pf.FacebookAccessToken = null;
        m_pf.FCommit();
        ShowFacebookState(m_pf.CanPostFacebook());
    }

    protected void lnkDeAuthGooglePlus_Click(object sender, EventArgs e)
    {
        m_pf.GooglePlusAccessToken = "";
        m_pf.FCommit();
        ShowGooglePlusState(m_pf.CanPostGooglePlus());
    }

    protected void lnkSetUpFacebook_Click(object sender, EventArgs e)
    {
        MFBFacebook.Authorize();
    }

    protected void lnkSetUpTwitter_Click(object sender, EventArgs e)
    {
        try
        {
            SocialNetworkAuthorization.PushRedirect(Request.Url.PathAndQuery);
            Response.Redirect(mfbTwitter1.AuthURL.ToString());
        }
        catch (MyFlightbookException ex)
        {
            lblSocNetworkPrefsUpdated.Text = ex.Message;
            lblSocNetworkPrefsUpdated.CssClass = "error";
        }
    }

    protected void lnkAuthorizeGooglePlus_Click(object sender, EventArgs e)
    {
        try
        {
            SocialNetworkAuthorization.PushRedirect(Request.Url.PathAndQuery);
            Response.Redirect(mfbGooglePlus1.AuthURL.ToString());
        }
        catch (MyFlightbookException ex)
        {
            lblSocNetworkPrefsUpdated.Text = ex.Message;
            lblSocNetworkPrefsUpdated.CssClass = "error";
        }
    }
    #endregion

    #region Cloud Storage
    protected void lnkAuthDropbox_Click(object sender, EventArgs e)
    {
        Uri uri = new Uri(String.Format(CultureInfo.InvariantCulture, "{0}://{1}{2}?{3}=1",
            Request.IsLocal && !Request.IsSecureConnection ? "http" : "https",
            Request.Url.Host,
            ResolveUrl("~/Member/EditProfile.aspx/pftPrefs"),
            szParamDropboxAuth));
        new MFBDropbox().Authorize(uri);
    }

    protected void lnkDeAuthDropbox_Click(object sender, EventArgs e)
    {
        m_pf.DropboxAccessToken = null;
        m_pf.FCommit();
        mvDropBoxState.SetActiveView(vwAuthDropBox);
    }

    protected void lnkAuthorizeOneDrive_Click(object sender, EventArgs e)
    {
        Uri uri = new Uri(String.Format(CultureInfo.InvariantCulture, "http://{0}/logbook/public/OneDriveRedir.aspx", Request.Url.Host));
        new OneDrive().Authorize(uri);
    }

    protected void lnkDeAuthOneDrive_Click(object sender, EventArgs e)
    {
        m_pf.OneDriveAccessToken = null;
        m_pf.FCommit();
        mvOneDriveState.SetActiveView(vwAuthOneDrive);
    }

    protected void lnkAuthorizeGDrive_Click(object sender, EventArgs e)
    {
        Uri uri = new Uri(String.Format(CultureInfo.InvariantCulture, "https://{0}{1}?{2}=1",
            Request.Url.Host,
            Request.Url.AbsolutePath,
            szParamGDriveAuth));
        new GoogleDrive().Authorize(uri);
    }

    protected void lnkDeAuthGDrive_Click(object sender, EventArgs e)
    {
        m_pf.GoogleDriveAccessToken = null;
        m_pf.FCommit();
        mvGDriveState.SetActiveView(vwAuthGDrive);
    }

    protected void cmbDefaultCloud_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            m_pf.DefaultCloudStorage = (StorageID)Enum.Parse(typeof(StorageID), cmbDefaultCloud.SelectedValue);
            m_pf.FCommit();
        }
        catch (ArgumentNullException) { }
        catch (ArgumentException) { }
        catch (OverflowException) { }
    }

    protected void rblCloudBackupAppendDate_SelectedIndexChanged(object sender, EventArgs e)
    {
        m_pf.OverwriteCloudBackup = Convert.ToBoolean(rblCloudBackupAppendDate.SelectedValue, CultureInfo.InvariantCulture);
        m_pf.FCommit();
    }
    #endregion // Social Networking

    #region Property Blacklist
    protected void UpdateBlacklist(bool fForceDB = false)
    {
        rptUsedProps.DataSource = Array.FindAll(CustomPropertyType.GetCustomPropertyTypes(Page.User.Identity.Name, fForceDB), cpt => cpt.IsFavorite);
        rptUsedProps.DataBind();
        rptBlackList.DataSource = CustomPropertyType.GetCustomPropertyTypes(m_pf.BlacklistedProperties);
        rptBlackList.DataBind();
    }

    protected void btnWhiteList_Click(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(txtPropID.Text) && !String.IsNullOrEmpty(txtPropID.Text.Trim()))
        {
            try
            {
                int idPropType = Convert.ToInt32(txtPropID.Text, CultureInfo.InvariantCulture);
                m_pf.BlacklistedProperties.RemoveAll(id => id == idPropType);
                m_pf.FCommit();
                UpdateBlacklist(true);
            }
            catch
            {
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Error Parsing proptype '{0}' for whitelist in invariant culture.  Current culture is {1}.", txtPropID.Text, CultureInfo.CurrentCulture.DisplayName));
            }
        }
    }

    protected void btnBlackList_Click(object sender, EventArgs e)
    {
        if (!String.IsNullOrEmpty(txtPropID.Text) && !String.IsNullOrEmpty(txtPropID.Text.Trim()))
        {
            try
            {
                int idPropType = Convert.ToInt32(txtPropID.Text, CultureInfo.InvariantCulture);
                if (!m_pf.BlacklistedProperties.Contains(idPropType))
                {
                    m_pf.BlacklistedProperties.Add(idPropType);
                    m_pf.FCommit();
                    UpdateBlacklist(true);
                }
            }
            catch
            {
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Error Parsing proptype '{0}' for whitelist in invariant culture.  Current culture is {1}.", txtPropID.Text, CultureInfo.CurrentCulture.DisplayName));
            }
    }
}
    #endregion

    #endregion // Preferences

    #region Pilot Information
    protected Boolean FCommitPilotInfo()
    {
        m_pf.License = txtLicense.Text;
        m_pf.Certificate = txtCertificate.Text;
        if (m_pf.Certificate.Length > 30)
            m_pf.Certificate = m_pf.Certificate.Substring(0, 30);
        m_pf.CertificateExpiration = mfbTypeInDateCFIExpiration.Date;
        m_pf.EnglishProficiencyExpiration = mfbDateEnglishCheck.Date;

        try
        {
            m_pf.FCommit();
        }
        catch (MyFlightbookException ex)
        {
            lblPilotInfoUpdated.Visible = true;
            lblPilotInfoUpdated.Text = ex.Message;
            lblPilotInfoUpdated.CssClass = "error";
            return false;
        }

        return true;
    }

    protected bool FCommitMedicalInfo()
    {
        Page.Validate("valPilotInfo");
        if (!IsValid)
            return false;

        m_pf.LastMedical = dateMedical.Date;
        m_pf.MonthsToMedical = Convert.ToInt32(cmbMonthsMedical.SelectedValue, CultureInfo.InvariantCulture);
        m_pf.UsesICAOMedical = rblMedicalDurationType.SelectedIndex > 0;

        try
        {
            m_pf.FCommit();
        }
        catch (MyFlightbookException ex)
        {
            lblMedicalInfo.Visible = true;
            lblMedicalInfo.Text = ex.Message;
            lblMedicalInfo.CssClass = "error";
            return false;
        }

        return true;
    }

    protected void btnUpdatePilotInfo_Click(object sender, EventArgs e)
    {
        if (FCommitPilotInfo())
        {
            lblPilotInfoUpdated.Visible = true;
        }
    }

    protected void btnUpdateMedical_Click(object sender, EventArgs e)
    {
        if (FCommitMedicalInfo())
        {
            lblMedicalInfo.Visible = true;
            UpdateNextMedical(m_pf);
        }
    }

    private void UpdateNextMedical(Profile pf)
    {
        if (pf.LastMedical.CompareTo(DateTime.MinValue) != 0)
        {
            lblNextMedical.Text = pf.NextMedical.ToShortDateString();
            pnlNextMedical.Visible = true;
        }
        else
            pnlNextMedical.Visible = false;
    }

    public void DurationIsValid(object source, ServerValidateEventArgs args)
    {
        if (args == null)
            throw new ArgumentNullException("args");
        args.IsValid = (dateMedical.Date == DateTime.MinValue || cmbMonthsMedical.SelectedValue != "0");
    }
    #endregion
}
