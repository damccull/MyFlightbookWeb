<%@ Control Language="C#" AutoEventWireup="true" CodeFile="mfbCurrency.ascx.cs" Inherits="Controls_mfbCurrency" %>
<%@ Register Src="mfbSimpleTotals.ascx" TagName="mfbSimpleTotals" TagPrefix="uc2" %>
<span style="font-size:9px;"><asp:HyperLink ID="lnkDisclaimer" runat="server" Target="_blank" Text="<%$ Resources:Currency, DisclaimerLink %>" NavigateUrl="~/Public/CurrencyDisclaimer.aspx"></asp:HyperLink></span>
<asp:GridView ID="gvCurrency" CellPadding="2" CellSpacing="1" ShowHeader="false" runat="server" GridLines="None" AutoGenerateColumns="false" OnRowDataBound="gvCurrency_RowDataBound">
    <Columns>
        <asp:TemplateField ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <div><asp:Label ID="lblTitle" runat="server" CssClass="<%# CssCurrencyLabel %>" Text='<%# Eval("Attribute") %>'></asp:Label></div>
            </ItemTemplate>
        </asp:TemplateField>
        <asp:TemplateField ItemStyle-VerticalAlign="Top">
            <ItemTemplate>
                <div><asp:Label ID="lblStatus" runat="server" CssClass='<%# CSSForItem((MyFlightbook.FlightCurrency.CurrencyState) Eval("Status")) %>' Text='<%# Eval("Value") %>'></asp:Label></div>
                <div><asp:Label ID="lblDiscrepancy" CssClass="<%# CssCurrencyGap %>" runat="server" Text='<%# Eval("Discrepancy") %>'></asp:Label></div>
            </ItemTemplate>
        </asp:TemplateField>
    </Columns>
    <EmptyDataTemplate>
        <p><asp:Literal ID="Literal2" runat="server" Text="<%$ Resources:Currency, NoStatus %>"></asp:Literal></p>
    </EmptyDataTemplate>
</asp:GridView>
<asp:Label ID="lblError" runat="server"></asp:Label>
