<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Guardias.aspx.cs" Inherits="Guardias.Guardias" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Sistema de Guardias</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0;
            font-family: 'Segoe UI', Roboto, Arial, sans-serif; }

        body { background: #f7f8fa; color: #1f2937; padding: 40px 24px;
               min-height: 100vh; font-size: 14px; line-height: 1.5; }

        .contenedor { max-width: 1080px; margin: 0 auto; }

        .header { margin-bottom: 32px; }
        .header h1 { font-size: 22px; font-weight: 600; color: #111827; letter-spacing: -0.01em; }
        .header p { font-size: 13px; color: #6b7280; margin-top: 4px; }

        .card { background: #ffffff; border: 1px solid #e5e7eb; border-radius: 10px;
                padding: 28px; margin-bottom: 20px; }
        .card-titulo { font-size: 13px; font-weight: 600; color: #374151;
                       margin-bottom: 22px; padding-bottom: 14px; border-bottom: 1px solid #f0f1f3; }

        .grid-form { display: grid; grid-template-columns: repeat(3, 1fr); gap: 18px; }
        .campo { display: flex; flex-direction: column; }
        .campo.full { grid-column: 1 / -1; }
        label { font-size: 12px; font-weight: 500; color: #6b7280; margin-bottom: 7px; }

        input[type=text], input[type=datetime-local], input[type=number], textarea {
            padding: 10px 12px; border: 1px solid #d1d5db; border-radius: 7px;
            font-size: 14px; background: #ffffff; color: #111827;
            transition: border-color .15s, box-shadow .15s; }
        input::placeholder { color: #9ca3af; }
        input:focus, textarea:focus { outline: none; border-color: #111827;
            box-shadow: 0 0 0 3px rgba(17,24,39,.06); }
        textarea { resize: vertical; min-height: 68px; }

        .dur-box { margin-top: 16px; font-size: 13px; color: #4b5563;
                   background: #f3f4f6; border: 1px solid #e5e7eb; border-radius: 7px;
                   padding: 9px 13px; display: inline-block; }
        .dur-box b { color: #111827; }

        .acciones { margin-top: 24px; padding-top: 20px; border-top: 1px solid #f0f1f3;
                    display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
        .btn { border: 1px solid transparent; border-radius: 7px; padding: 10px 20px;
               font-size: 13.5px; font-weight: 500; cursor: pointer;
               transition: background .15s, border-color .15s; }
        .btn-guardar { background: #111827; color: #ffffff; }
        .btn-guardar:hover { background: #374151; }
        .btn-limpiar { background: #ffffff; color: #374151; border-color: #d1d5db; }
        .btn-limpiar:hover { background: #f9fafb; }

        .chk { display: flex; align-items: center; gap: 8px; font-size: 13px;
               color: #4b5563; margin-left: 6px; }
        .chk input { width: 15px; height: 15px; accent-color: #111827; }
        .msg { font-weight: 500; font-size: 13px; margin-left: auto; }

        table.gv { width: 100%; border-collapse: collapse; font-size: 13.5px; }
        table.gv th { background: #fafafa; color: #6b7280; padding: 12px 14px;
            text-align: left; font-weight: 500; font-size: 12px; border-bottom: 1px solid #e5e7eb; }
        table.gv td { padding: 13px 14px; border-bottom: 1px solid #f0f1f3; color: #374151; }
        table.gv tr:last-child td { border-bottom: none; }
        table.gv tr:hover td { background: #fafafa; }
        .lnk { color: #374151; text-decoration: none; font-weight: 500; }
        .lnk:hover { color: #111827; text-decoration: underline; }
        .lnk-rojo { color: #b91c1c; } .lnk-rojo:hover { color: #991b1b; }
        .lnk-azul { color: #1d4ed8; } .lnk-azul:hover { color: #1e40af; }
        .sep { color: #d1d5db; margin: 0 4px; }

        @media (max-width: 760px) { .grid-form { grid-template-columns: 1fr; } }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="contenedor">

            <div class="header">
                <h1>Sistema de Guardias</h1>
                <p>Registro de turnos, horarios y secuencia de relevos</p>
            </div>

            <!-- ============ FORMULARIO ============ -->
            <div class="card">
                <div class="card-titulo">Registrar / Editar guardia</div>
                <asp:HiddenField ID="hfId" runat="server" />
                <asp:HiddenField ID="hfReemplazoId" runat="server" />
                <div class="grid-form">
                    <div class="campo">
                        <label>Nombre del guardia</label>
                        <asp:TextBox ID="txtNombre" runat="server" />
                    </div>
                    <div class="campo">
                        <label>Inicio del turno (fecha y hora)</label>
                        <asp:TextBox ID="txtInicio" runat="server" TextMode="DateTimeLocal" ClientIDMode="Static" />
                    </div>
                    <div class="campo">
                        <label>Fin del turno (fecha y hora)</label>
                        <asp:TextBox ID="txtFin" runat="server" TextMode="DateTimeLocal" ClientIDMode="Static" />
                    </div>
                    <div class="campo">
                        <label>Posici&oacute;n / turno (opcional)</label>
                        <asp:TextBox ID="txtPosicion" runat="server" TextMode="Number"
                            placeholder="vac&iacute;o = al final" />
                    </div>
                    <div class="campo full">
                        <label>Observaciones</label>
                        <asp:TextBox ID="txtObservaciones" runat="server" TextMode="MultiLine" />
                    </div>
                </div>

                <div class="dur-box">Duraci&oacute;n del turno: <b><span id="lblDur">&mdash;</span></b></div>

                <div class="acciones">
                    <asp:Button ID="btnGuardar" runat="server" Text="Guardar guardia"
                        CssClass="btn btn-guardar" OnClick="btnGuardar_Click" />
                    <asp:Button ID="btnLimpiar" runat="server" Text="Limpiar"
                        CssClass="btn btn-limpiar" OnClick="btnLimpiar_Click" />
                    <label class="chk">
                        <asp:CheckBox ID="chkMedio" runat="server" />
                        Insertar al medio del rol (los dem&aacute;s se corren a la derecha)
                    </label>
                    <asp:Label ID="lblMensaje" runat="server" CssClass="msg" />
                </div>
            </div>

            <!-- ============ TABLA ============ -->
            <div class="card">
                <div class="card-titulo">Secuencia de guardias y relevos</div>
                <asp:GridView ID="gvGuardias" runat="server" AutoGenerateColumns="false"
                    DataKeyNames="IdGuardia" CssClass="gv" GridLines="None"
                    OnRowCommand="gvGuardias_RowCommand"
                    EmptyDataText="No hay guardias registrados.">
                    <Columns>
                        <asp:BoundField DataField="Posicion"      HeaderText="Turno" />
                        <asp:BoundField DataField="Nombre"        HeaderText="Trabajador" />
                        <asp:BoundField DataField="InicioTurno"   HeaderText="Inicio del turno" />
                        <asp:BoundField DataField="FinTurno"      HeaderText="Fin del turno" />
                        <asp:BoundField DataField="DuracionHoras" HeaderText="Duraci&oacute;n (h)" />
                        <asp:BoundField DataField="Observaciones" HeaderText="Observaciones" />
                        <asp:TemplateField HeaderText="Acciones">
                            <ItemTemplate>
                                <asp:LinkButton runat="server" CssClass="lnk" Text="Editar"
                                    CommandName="EditarG" CommandArgument='<%# Container.DataItemIndex %>' />
                                <span class="sep">|</span>
                                <asp:LinkButton runat="server" CssClass="lnk lnk-azul" Text="Reemplazar"
                                    CommandName="ReemplazarG" CommandArgument='<%# Container.DataItemIndex %>' />
                                <span class="sep">|</span>
                                <asp:LinkButton runat="server" CssClass="lnk lnk-rojo" Text="Se va"
                                    CommandName="SeVaG" CommandArgument='<%# Container.DataItemIndex %>'
                                    OnClientClick="return confirm('Este guardia se va. Los dem\u00e1s avanzar\u00e1n de turno. Continuar?');" />
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>

        </div>

        <script type="text/javascript">
            // Calcula y muestra la duración del turno en vivo
            function calcularDuracion() {
                var i = document.getElementById('txtInicio');
                var f = document.getElementById('txtFin');
                var el = document.getElementById('lblDur');
                if (!i || !f || !el) return;
                if (i.value && f.value) {
                    var d1 = new Date(i.value), d2 = new Date(f.value);
                    var horas = (d2 - d1) / 3600000;
                    if (isNaN(horas) || horas <= 0) { el.textContent = '\u2014'; return; }
                    el.textContent = horas.toFixed(1) + ' h';
                } else { el.textContent = '\u2014'; }
            }
            document.addEventListener('input', function (e) {
                if (e.target && (e.target.id === 'txtInicio' || e.target.id === 'txtFin'))
                    calcularDuracion();
            });
            window.addEventListener('load', calcularDuracion);
        </script>
    </form>
</body>
</html>
