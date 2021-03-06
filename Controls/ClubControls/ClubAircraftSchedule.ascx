﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ClubAircraftSchedule.ascx.cs" Inherits="Controls_ClubControls_ClubAircraftSchedule" %>
<%@ Register src="../mfbResourceSchedule.ascx" tagname="mfbResourceSchedule" tagprefix="uc1" %>
<asp:FormView ID="fvClubaircraft" runat="server" CssClass="mfbDefault">
    <ItemTemplate>
        <uc1:mfbResourceSchedule ID="mfbResourceSchedule1" NavInitClientFunction="InitClubNav" Mode='<%# Mode %>' HideNavContainer="true" ResourceHeader='<%# Eval("DisplayTailnumberWithModel") %>'
            ResourceID='<%# Eval("AircraftID") %>' ClubID='<%# Eval("ClubID") %>' runat="server">
            <ResourceTemplate>
                <asp:Panel ID="pnlSampleImage" Visible='<%# Eval("HasSampleImage") %>' Width="200px" runat="server" style="text-align:center;">
                    <asp:HyperLink ID="lnkImage" runat="server" NavigateUrl='<%# Eval("SampleImageFull") %>' Target="_blank">
                        <asp:Image ID="imgSample" ImageUrl='<%# Eval("SampleImageThumbnail") %>' ImageAlign="Middle" runat="server" />
                    </asp:HyperLink>
                    <div><%# Eval("SampleImageComment") %></div>
                    <br />
                </asp:Panel>
                <div><%# Eval("ClubDescription") %></div>
            </ResourceTemplate>
        </uc1:mfbResourceSchedule>
    </ItemTemplate>
</asp:FormView>


