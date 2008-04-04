<%@ Page Language="C#" %>
<script runat="server">
  protected override void OnLoad(EventArgs e)
  {
    Response.Redirect("~/home2/index.castle");
    base.OnLoad(e);
  }
</script>
