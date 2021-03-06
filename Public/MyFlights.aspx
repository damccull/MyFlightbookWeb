﻿<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    CodeFile="MyFlights.aspx.cs" Inherits="Public_MyFlights" %>
<%@ MasterType VirtualPath="~/MasterPage.master" %>
<%@ Register Src="../Controls/mfbImageList.ascx" TagName="mfbImageList" TagPrefix="uc1" %>
<%@ Register src="../Controls/fbComment.ascx" tagname="fbComment" tagprefix="uc3" %>
<%@ Register src="../Controls/mfbPublicFlightItem.ascx" tagname="mfbPublicFlightItem" tagprefix="uc4" %>
<asp:Content ID="Content1" ContentPlaceHolderID="cpMain" runat="Server">
    <script type="text/javascript" src="https://code.jquery.com/jquery-1.10.1.min.js"></script>
    <script type="text/javascript" src='<%= ResolveUrl("~/public/endless-scroll.js") %>'></script>
    <script type="text/javascript" src='<%= ResolveUrl("~/public/jquery.json-2.4.min.js") %>'></script>
    <h1>
        <asp:Label ID="lblHeader" runat="server" Text=""></asp:Label></h1>
    <asp:GridView ID="gvMyFlights" runat="server" OnRowDataBound="gvMyFlights_rowDataBound"
        GridLines="None" Visible="true" AutoGenerateColumns="false" CellPadding="5" EnableViewState="false"
        AllowPaging="false" AllowSorting="false" ShowFooter="false" ShowHeader="false" >
        <HeaderStyle HorizontalAlign="Left" />
        <Columns>
            <asp:TemplateField>
                <ItemStyle VerticalAlign="Top" />
                <ItemTemplate>
                    <uc4:mfbPublicFlightItem ID="mfbPublicFlightItem1" runat="server" />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <EmptyDataTemplate>
            <p><asp:Label ID="lblNoneFound" Font-Bold="true" runat="server" Text="<%$ Resources:LogbookEntry, PublicFlightNoneFound %>"></asp:Label></p>
        </EmptyDataTemplate>
    </asp:GridView>

    <script type="text/javascript">
        var params = new Object();
        params.skip = "<%=PageSize %>";
        params.pageSize = "<%=PageSize %>";
        params.szUser = "<%=UserName %>";

        $(document).ready(function () {

            $(document).endlessScroll(
        {
            bottomPixels: 300,
            fireOnce: true,
            fireDelay: 2000,
            callback: function (p) {

                // ajax call to fetch next set of rows 
                $.ajax(
                {
                    type: "POST",
                    data: $.toJSON(params),
                    url: "MyFlights.aspx/HtmlRowsForFlights",
                    dataType: "json",
                    contentType: "application/json",
                    error: function (response) { alert(result.status + ' ' + result.statusText); },
                    complete: function (response) { params.skip = parseInt(params.skip) + parseInt(params.pageSize); },
                    success: function (response) {
                        var FlightRows = response.d;
                        // populate the rows 
                        for (i = 0; i < FlightRows.length; i++) {
                            // Append the row (which is raw HTML), and parse it
                            // We have to parse it for the "Comment" tag to show up.
                            $("#<% =gvMyFlights.ClientID %>").append(FlightRows[i].HTMLRowText);
                         }
                    }
                }
                );
            }
        }
        );
        });
  </script>
</asp:Content>
