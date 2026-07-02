using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using MySql.Data.MySqlClient;

namespace Guardias
{
    public partial class Guardias : System.Web.UI.Page
    {
        private readonly string cs =
            ConfigurationManager.ConnectionStrings["GuardiasDB"].ConnectionString;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                CargarGuardias();
            }
        }

        // ============================================================
        //  CARGAR / LISTAR
        // ============================================================
        private void CargarGuardias()
        {
            using (MySqlConnection con = new MySqlConnection(cs))
            {
                string q = @"SELECT IdGuardia,
                                    Nombre,
                                    DATE_FORMAT(InicioTurno,'%d/%m/%Y %H:%i') AS InicioTurno,
                                    DATE_FORMAT(FinTurno   ,'%d/%m/%Y %H:%i') AS FinTurno,
                                    ROUND(TIMESTAMPDIFF(MINUTE, InicioTurno, FinTurno) / 60, 1) AS DuracionHoras,
                                    Posicion,
                                    Observaciones
                             FROM Guardias
                             WHERE Activo = 1
                             ORDER BY Posicion";
                MySqlDataAdapter da = new MySqlDataAdapter(q, con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                gvGuardias.DataSource = dt;
                gvGuardias.DataBind();
            }
        }

        // ============================================================
        //  GUARDAR (nuevo, editar o reemplazo)
        // ============================================================
        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            // --- Validaciones ---
            if (txtNombre.Text.Trim() == "")
            {
                Mensaje("Ingrese el nombre del guardia.", true);
                return;
            }
            if (txtInicio.Text == "" || txtFin.Text == "")
            {
                Mensaje("Ingrese el inicio y el fin del turno.", true);
                return;
            }

            DateTime dIni, dFin;
            bool okIni = DateTime.TryParse(txtInicio.Text, CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out dIni);
            bool okFin = DateTime.TryParse(txtFin.Text, CultureInfo.InvariantCulture,
                                           DateTimeStyles.None, out dFin);
            if (!okIni || !okFin)
            {
                Mensaje("Fecha u hora inválida.", true);
                return;
            }
            if (dFin <= dIni)
            {
                Mensaje("El fin del turno debe ser posterior al inicio.", true);
                return;
            }

            string ini = txtInicio.Text.Replace("T", " ");
            string fin = txtFin.Text.Replace("T", " ");

            using (MySqlConnection con = new MySqlConnection(cs))
            {
                con.Open();

                // ---------- MODO REEMPLAZO ----------
                // El nuevo entra en la posición del guardia objetivo; ese guardia
                // NO se elimina: baja una posición y su horario se recorre en cadena.
                if (!string.IsNullOrEmpty(hfReemplazoId.Value))
                {
                    int idObjetivo = int.Parse(hfReemplazoId.Value);
                    int pos = ObtenerPosicion(con, idObjetivo);

                    // 1) Todos los de esa posición en adelante bajan un puesto
                    MySqlCommand shift = new MySqlCommand(
                        "UPDATE Guardias SET Posicion = Posicion + 1 " +
                        "WHERE Activo = 1 AND Posicion >= @p", con);
                    shift.Parameters.AddWithValue("@p", pos);
                    shift.ExecuteNonQuery();

                    // 2) El nuevo guardia ocupa el puesto liberado, con su horario
                    InsertarGuardia(con, ini, fin, pos);

                    // 3) Los desplazados recorren su horario: cada uno empieza cuando
                    //    termina el anterior, conservando sus propias horas de trabajo
                    ReencadenarDesde(con, pos);

                    hfReemplazoId.Value = "";
                }
                // ---------- EDITAR ----------
                else if (!string.IsNullOrEmpty(hfId.Value))
                {
                    int idEdit = int.Parse(hfId.Value);

                    if (HayCruce(con, ini, fin, idEdit))
                    {
                        Mensaje("Ese horario ya está ocupado por otro guardia.", true);
                        return;
                    }

                    MySqlCommand upd = new MySqlCommand(
                        @"UPDATE Guardias SET
                            Nombre = @n, InicioTurno = @ini, FinTurno = @fin, Observaciones = @obs
                          WHERE IdGuardia = @id", con);
                    upd.Parameters.AddWithValue("@n", txtNombre.Text.Trim());
                    upd.Parameters.AddWithValue("@ini", ini);
                    upd.Parameters.AddWithValue("@fin", fin);
                    upd.Parameters.AddWithValue("@obs", txtObservaciones.Text.Trim());
                    upd.Parameters.AddWithValue("@id", idEdit);
                    upd.ExecuteNonQuery();
                }
                // ---------- NUEVO ----------
                else
                {
                    // Si el horario choca con otro guardia -> error
                    if (HayCruce(con, ini, fin, 0))
                    {
                        Mensaje("Ese horario ya está ocupado. Si va a cubrir a alguien, use 'Reemplazar'.", true);
                        return;
                    }

                    int total = ContarGuardias(con);
                    int posicion;
                    if (chkMedio.Checked)
                        posicion = (total / 2) + 1;
                    else if (!string.IsNullOrEmpty(txtPosicion.Text))
                        posicion = int.Parse(txtPosicion.Text);
                    else
                        posicion = total + 1;

                    if (posicion < 1) posicion = 1;
                    if (posicion > total + 1) posicion = total + 1;

                    MySqlCommand shift = new MySqlCommand(
                        "UPDATE Guardias SET Posicion = Posicion + 1 " +
                        "WHERE Activo = 1 AND Posicion >= @p", con);
                    shift.Parameters.AddWithValue("@p", posicion);
                    shift.ExecuteNonQuery();

                    InsertarGuardia(con, ini, fin, posicion);
                }
            }

            LimpiarFormulario();
            CargarGuardias();
        }

        // ============================================================
        //  MÉTODOS AUXILIARES
        // ============================================================

        // ¿El nuevo horario se cruza con el de algún guardia activo?
        // Solape: (inicioNuevo < finExistente) Y (finNuevo > inicioExistente)
        private bool HayCruce(MySqlConnection con, string ini, string fin, int excluirId)
        {
            MySqlCommand c = new MySqlCommand(
                @"SELECT COUNT(*) FROM Guardias
                  WHERE Activo = 1
                    AND IdGuardia <> @ex
                    AND @ini < FinTurno
                    AND @fin > InicioTurno", con);
            c.Parameters.AddWithValue("@ex", excluirId);
            c.Parameters.AddWithValue("@ini", ini);
            c.Parameters.AddWithValue("@fin", fin);
            return Convert.ToInt32(c.ExecuteScalar()) > 0;
        }

        // Recorre el horario de los guardias a partir de una posición:
        // el primero (el nuevo) queda fijo, y cada siguiente empieza cuando
        // termina el anterior, manteniendo su propia duración de horas.
        private void ReencadenarDesde(MySqlConnection con, int posDesde)
        {
            List<int> ids = new List<int>();
            List<DateTime> inicios = new List<DateTime>();
            List<DateTime> fines = new List<DateTime>();

            MySqlCommand sel = new MySqlCommand(
                @"SELECT IdGuardia, InicioTurno, FinTurno FROM Guardias
                  WHERE Activo = 1 AND Posicion >= @p ORDER BY Posicion", con);
            sel.Parameters.AddWithValue("@p", posDesde);
            using (MySqlDataReader dr = sel.ExecuteReader())
            {
                while (dr.Read())
                {
                    ids.Add(Convert.ToInt32(dr["IdGuardia"]));
                    inicios.Add(Convert.ToDateTime(dr["InicioTurno"]));
                    fines.Add(Convert.ToDateTime(dr["FinTurno"]));
                }
            }

            if (ids.Count <= 1) return; // solo está el nuevo, nada que recorrer

            DateTime finAnterior = fines[0]; // fin del guardia nuevo (queda fijo)
            for (int i = 1; i < ids.Count; i++)
            {
                TimeSpan duracion = fines[i] - inicios[i]; // conserva sus horas
                DateTime nuevoInicio = finAnterior;
                DateTime nuevoFin = nuevoInicio + duracion;

                MySqlCommand upd = new MySqlCommand(
                    "UPDATE Guardias SET InicioTurno = @ini, FinTurno = @fin WHERE IdGuardia = @id", con);
                upd.Parameters.AddWithValue("@ini", nuevoInicio.ToString("yyyy-MM-dd HH:mm:ss"));
                upd.Parameters.AddWithValue("@fin", nuevoFin.ToString("yyyy-MM-dd HH:mm:ss"));
                upd.Parameters.AddWithValue("@id", ids[i]);
                upd.ExecuteNonQuery();

                finAnterior = nuevoFin;
            }
        }

        private void InsertarGuardia(MySqlConnection con, string ini, string fin, int posicion)
        {
            MySqlCommand ins = new MySqlCommand(
                @"INSERT INTO Guardias
                    (Nombre, InicioTurno, FinTurno, Posicion, Observaciones, Activo)
                  VALUES (@n, @ini, @fin, @pos, @obs, 1)", con);
            ins.Parameters.AddWithValue("@n", txtNombre.Text.Trim());
            ins.Parameters.AddWithValue("@ini", ini);
            ins.Parameters.AddWithValue("@fin", fin);
            ins.Parameters.AddWithValue("@pos", posicion);
            ins.Parameters.AddWithValue("@obs", txtObservaciones.Text.Trim());
            ins.ExecuteNonQuery();
        }

        private int ObtenerPosicion(MySqlConnection con, int id)
        {
            MySqlCommand c = new MySqlCommand(
                "SELECT Posicion FROM Guardias WHERE IdGuardia = @id", con);
            c.Parameters.AddWithValue("@id", id);
            return Convert.ToInt32(c.ExecuteScalar());
        }

        private void MarcarInactivo(MySqlConnection con, int id)
        {
            MySqlCommand c = new MySqlCommand(
                "UPDATE Guardias SET Activo = 0 WHERE IdGuardia = @id", con);
            c.Parameters.AddWithValue("@id", id);
            c.ExecuteNonQuery();
        }

        private int ContarGuardias(MySqlConnection con)
        {
            MySqlCommand c = new MySqlCommand(
                "SELECT COUNT(*) FROM Guardias WHERE Activo = 1", con);
            return Convert.ToInt32(c.ExecuteScalar());
        }

        // ============================================================
        //  ACCIONES DE LA TABLA (Editar / Reemplazar / Se va)
        // ============================================================
        protected void gvGuardias_RowCommand(object sender,
            System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            int index = Convert.ToInt32(e.CommandArgument);
            int id = Convert.ToInt32(gvGuardias.DataKeys[index].Value);

            if (e.CommandName == "EditarG")
            {
                CargarEnFormulario(id);
            }
            else if (e.CommandName == "ReemplazarG")
            {
                CargarParaReemplazo(id);
            }
            else if (e.CommandName == "SeVaG")
            {
                using (MySqlConnection con = new MySqlConnection(cs))
                {
                    con.Open();
                    MarcarInactivo(con, id);
                    RecalcularPosiciones(con);
                }
                LimpiarFormulario();
                CargarGuardias();
            }
        }

        // Cargar datos para EDITAR
        private void CargarEnFormulario(int id)
        {
            LeerEnFormulario(id, esReemplazo: false);
            Mensaje("Editando guardia. Modifique y presione Guardar.", false);
        }

        // Cargar datos para REEMPLAZAR: se copia el horario del objetivo como
        // punto de partida y se limpia el nombre para el guardia nuevo.
        private void CargarParaReemplazo(int id)
        {
            string nombreObjetivo = LeerEnFormulario(id, esReemplazo: true);
            Mensaje("Reemplazando a " + nombreObjetivo +
                    ". El nuevo entra en su puesto y " + nombreObjetivo +
                    " baja de turno. Escriba el nuevo guardia y presione Guardar.", false);
        }

        // Lee un guardia y llena el formulario. Devuelve el nombre leído.
        private string LeerEnFormulario(int id, bool esReemplazo)
        {
            string nombre = "";
            using (MySqlConnection con = new MySqlConnection(cs))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand(
                    @"SELECT Nombre,
                             DATE_FORMAT(InicioTurno,'%Y-%m-%dT%H:%i') AS InicioTurno,
                             DATE_FORMAT(FinTurno   ,'%Y-%m-%dT%H:%i') AS FinTurno,
                             Posicion, Observaciones
                      FROM Guardias WHERE IdGuardia = @id", con);
                cmd.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        nombre = dr["Nombre"].ToString();
                        txtInicio.Text = dr["InicioTurno"].ToString();
                        txtFin.Text = dr["FinTurno"].ToString();
                        txtPosicion.Text = dr["Posicion"].ToString();

                        if (esReemplazo)
                        {
                            hfId.Value = "";
                            hfReemplazoId.Value = id.ToString();
                            txtNombre.Text = "";              // nuevo guardia
                            txtObservaciones.Text = "";
                        }
                        else
                        {
                            hfId.Value = id.ToString();
                            hfReemplazoId.Value = "";
                            txtNombre.Text = nombre;
                            txtObservaciones.Text = dr["Observaciones"].ToString();
                        }
                    }
                }
            }
            return nombre;
        }

        // Renumera posiciones 1,2,3... sin huecos (los demás avanzan)
        private void RecalcularPosiciones(MySqlConnection con)
        {
            List<int> ids = new List<int>();
            MySqlCommand sel = new MySqlCommand(
                "SELECT IdGuardia FROM Guardias WHERE Activo = 1 ORDER BY Posicion", con);
            using (MySqlDataReader dr = sel.ExecuteReader())
            {
                while (dr.Read()) ids.Add(Convert.ToInt32(dr["IdGuardia"]));
            }
            for (int i = 0; i < ids.Count; i++)
            {
                MySqlCommand upd = new MySqlCommand(
                    "UPDATE Guardias SET Posicion = @pos WHERE IdGuardia = @id", con);
                upd.Parameters.AddWithValue("@pos", i + 1);
                upd.Parameters.AddWithValue("@id", ids[i]);
                upd.ExecuteNonQuery();
            }
        }

        // ============================================================
        //  UTILIDADES DE UI
        // ============================================================
        protected void btnLimpiar_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        private void LimpiarFormulario()
        {
            hfId.Value = "";
            hfReemplazoId.Value = "";
            txtNombre.Text = "";
            txtInicio.Text = "";
            txtFin.Text = "";
            txtPosicion.Text = "";
            txtObservaciones.Text = "";
            chkMedio.Checked = false;
            lblMensaje.Text = "";
        }

        private void Mensaje(string texto, bool esError)
        {
            lblMensaje.Text = texto;
            lblMensaje.ForeColor = esError
                ? System.Drawing.Color.FromArgb(185, 28, 28)
                : System.Drawing.Color.FromArgb(75, 85, 99);
        }
    }
}