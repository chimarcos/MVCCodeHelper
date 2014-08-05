﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CHI_MVCCodeHelper
{
    public partial class MVCCodeHelperMain : Form
    {

        SqlConnection _dbConnection;
        public MVCCodeHelperMain()
        {
            InitializeComponent();
        }

        private void ViewModelGen_Load(object sender, EventArgs e)
        {
            string user = Settings.Get("User", "");
            string pass = Settings.Get("Password", "");
            string server = Settings.Get("Server", "");
            string db = Settings.Get("DBName", "");
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(server) || string.IsNullOrEmpty(db))
            {
                DBSettings.SelectedTab = tabPage1;
                MessageBox.Show("Set your DB settings before you start");
            }
            else
            {
                UserTB.Text = user;
                PassTB.Text = pass;
                ServeTB.Text = server;
                DatabaseCB.Text = db;
                _dbConnection = new SqlConnection("user id=" + user + ";" +
                                                 "password=" + pass + ";server=" + server + ";" +
                                                 "Trusted_Connection=false;" +
                                                 "database=" + db + "; " +
                                                 "connection timeout=30");
                _dbConnection.Open();
            }
        }



        private void TestB_Click(object sender, EventArgs e)
        {
            _dbConnection = new SqlConnection("user id=" + UserTB.Text + ";" +
                                      "password=" + PassTB.Text + ";server=" + ServeTB.Text + ";" +
                                      "Trusted_Connection=false;" +
                                      "database=" + DatabaseCB.SelectedText + "; " +
                                      "connection timeout=30");
            try
            {
                _dbConnection.Open();
                MessageBox.Show("Success!");
                Settings.Set("User", UserTB.Text);
                Settings.Set("Password", PassTB.Text);
                Settings.Set("Server", ServeTB.Text);
                Settings.Set("DBName", DatabaseCB.Text);

            }
            catch (Exception)
            {
                MessageBox.Show("Failed!");
            }

        }

        private void DatabaseCB_Click(object sender, EventArgs e)
        {
            SqlConnection DBConnection2 = new SqlConnection("user id=" + UserTB.Text + ";" +
                              "password=" + PassTB.Text + ";server=" + ServeTB.Text + ";" +
                              "Trusted_Connection=false;" +
                              "database=" + DatabaseCB.SelectedText + "; " +
                              "connection timeout=30");
            try
            {
                DatabaseCB.Items.Clear();



                DBConnection2.Open();

                List<String> databases = new List<String>();

                //get databases
                DataTable tblDatabases = DBConnection2.GetSchema("Databases");

                //add to list
                foreach (DataRow row in tblDatabases.Rows)
                {
                    String strDatabaseName = row["database_name"].ToString();

                    databases.Add(strDatabaseName);

                    DatabaseCB.Items.Add(strDatabaseName);
                }
            }
            catch (Exception)
            {

            }
            finally
            { DBConnection2.Close(); }
        }

        private void TableCB_Click(object sender, EventArgs e)
        {
            VMTableCB.Items.Clear();

            List<string> tables = new List<string>();

            DataTable dt = _dbConnection.GetSchema("Tables");
            foreach (DataRow row in dt.Rows)
            {
                if (((string)row[3]).Equals("BASE TABLE") && ((string)row[1]).Equals("dbo"))
                {
                    string tablename = (string)row[2];
                    tables.Add(tablename);
                }
            }
            tables.Sort();

            foreach (var item in tables)
            {
                VMTableCB.Items.Add(item);
            }

        }

        private void GenB_Click(object sender, EventArgs e)
        {
            if (VMTableCB.SelectedIndex == -1)
            {
                MessageBox.Show("Select a table first bro!");

            }
            else
            {
                string viewModelName = ViewModelNameTB.Text;
                string tableName = VMTableCB.SelectedItem.ToString();
                string tableNameplural = ToPlural(tableName);

                string[,] columns = new string[100, 4];
                const string n = "\n";

                string vmCode = "public  class " + viewModelName + n + "{" + n + n;

                string toEntity = @"public " + tableName + @" ToEntity()" + n + @"
            {" + n + @"
            var entity = new " + tableName + @"" + n + @"
            {" + n;
                string toEntityP = "public " + tableName + " ToEntity(" + tableName + " entity)" + "{" + n;
                string toModel = " public static " + viewModelName + " ToModel(" + tableName + " entity)" + "{" + n
                    + @" var model = new " + viewModelName + "();"
                    + n + " if (entity != null)" + n + "{" + n;





                SqlCommand cmd = new SqlCommand
                {
                    Connection = _dbConnection,
                    CommandText = "SELECT top 1 * FROM " + tableName
                };


                //Retrieve records from the Employees table into a DataReader.
                SqlDataReader myReader = cmd.ExecuteReader(CommandBehavior.KeyInfo);

                //Retrieve column schema into a DataTable.
                DataTable schemaTable = myReader.GetSchemaTable();
                int i = 0;
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {


                    columns[i, 0] = myField["ColumnName"].ToString();
                    columns[i, 1] = myField["DataType"].ToString();
                    columns[i, 2] = myField["ColumnSize"].ToString();
                    columns[i, 3] = myField["AllowDBNull"].ToString();

                    string columnName = myField["ColumnName"].ToString();

                    if ((columnName.Equals("createdDate") && !createdDate.Checked) ||
                        (columnName.Equals("modifiedDate") && !modifiedDate.Checked) ||
                        (columnName.Equals("createdBy") && !createdBy.Checked) ||
                        (columnName.Equals("modifiedBy") && !modifiedBy.Checked) ||
                        (columnName.Equals("rowVersion") && !rowVersion.Checked)
                        )
                    {
                        continue;
                    }


                    string columnNameCamel = columnName.First().ToString().ToUpper() + String.Join("", columnName.Skip(1));
                    columnNameCamel = columnNameCamel.Replace("ID", "Id");

                    string displayName = Regex.Replace(columnNameCamel, "(\\B[A-Z])", " $1");
                    string dataType = myField["DataType"].ToString().Replace("System.", "").Replace("Int16", "short").Replace("Int32", "int").Replace("Int", "int").Replace("String", "string").Replace("Boolean", "bool");
                    int size = Convert.ToInt32(myField["ColumnSize"]);
                    bool isNullable = Convert.ToBoolean(myField["AllowDBNull"]);
                    bool IsKey = Convert.ToBoolean(myField["IsKey"]);
                    if (isNullable)
                    {
                        if (!dataType.Equals("string"))
                        {
                            dataType = dataType + "?";
                        }
                    }
                    else if (RequiredAnn.Checked && !IsKey && !(columnName.Equals("createdDate") ||
                                                                columnName.Equals("modifiedDate") ||
                                                                columnName.Equals("createdBy") ||
                                                                columnName.Equals("modifiedBy") ||
                                                                columnName.Equals("rowVersion")))
                    {
                        vmCode = vmCode + @"[Required(ErrorMessage = ""{0} value is empty"")]" + n;
                    }

                    if (IsKey & HiddenInputAnn.Checked)
                    {
                        vmCode = vmCode + "[HiddenInput(DisplayValue = false)]" + n;
                    }
                    else if (DisplayAnn.Checked)
                    {
                        vmCode = vmCode + @"[Display(Name = """ + displayName + @""")]" + n;
                    }
                    if (dataType.Equals("string") & StringLengthAnn.Checked && size < 2147483647)
                    {
                        vmCode = vmCode + @"[StringLength(" + size + @", ErrorMessage = ""The length of {0} exceeds the limit of {1} characters!"")]" + n;
                    }

                    vmCode = vmCode + "public " + dataType + " " + columnNameCamel + " { get; set; }" + n + n;

                    toEntity = toEntity + columnName + " = " + columnNameCamel + "," + n;
                    toEntityP = toEntityP + "entity." + columnName + " = " + columnNameCamel + ";" + n;
                    toModel = toModel + "model." + columnNameCamel + " = entity." + columnName + ";" + n;
                    i++;
                }

                //Always close the DataReader and connection.
                myReader.Close();

                PK(tableName);

                foreach (DataGridViewRow itemm in PKGV.Rows)
                {
                    if (itemm.Cells[0].Value == null)
                    {
                        continue;
                    }

                    if (itemm.Cells[6].Value != null)
                    {
                        string FKTableName = itemm.Cells[6].Value.ToString();
                        string FKTableNamePlurar = FKTableName + "s";
                        if (FKTableName.Last().Equals('y'))
                        {
                            //agency agenc ies
                            FKTableNamePlurar = FKTableName.Remove(FKTableName.Length - 1) + "ies";
                        }
                        string FKTableNameVM = FKTableName + "ViewModel";
                        vmCode = vmCode + n + "public List<" + FKTableNameVM + "> " + FKTableNamePlurar + " { get; set; }" + n;

                    }
                }



                vmCode = vmCode + toEntity.TrimEnd(',') + n + @"};" + n + @"return entity;" + n + @"}" + n + n;
                vmCode = vmCode + toEntityP + n + @"return entity;" + n + @"}" + n + n;
                vmCode = vmCode + toModel + n + "}" + n + " return model;" + n + "}" + n + n;
                vmCode = vmCode + n + "}" + n;
                CodeText.Text = vmCode;
                CodeText.Focus();
                //  Clipboard.SetText(CodeText.Text);
            }
        }

        private void TableCB_SelectedValueChanged(object sender, EventArgs e)
        {
            ViewModelNameTB.Text = VMTableCB.SelectedItem + "VM";

        }

        private void RepoBtn_Click(object sender, EventArgs e)
        {

            if (TableGrid.Rows.Count == 0 || TableGrid.Rows[0].Cells[0].Value == null)
                MessageBox.Show("Add at least a table to the queue! \n Or make sure all table names are set in the queue.");


            var cmd = new SqlCommand();

            string repoCode = "";
            foreach (DataGridViewRow item in TableGrid.Rows)
            {
                if (item.Cells[0].Value == null)
                {
                    continue;
                }

                string tableName = item.Cells[0].Value.ToString();
                string regionName = item.Cells[1].Value.ToString();
                string viewModelName = tableName + "ViewModel";

                string tableNameplural = ToPlural(tableName);

                //Retrieve records from the Employees table into a DataReader.
                cmd.Connection = _dbConnection;
                cmd.CommandText = "SELECT top 1 * FROM " + tableName;
                SqlDataReader myReader = cmd.ExecuteReader(CommandBehavior.KeyInfo);
                //Retrieve column schema into a DataTable.
                DataTable schemaTable = myReader.GetSchemaTable();

                var columns = new string[100, 4];
                const string n = "\n";

                repoCode = repoCode + " #region " + regionName + n + n;

                string primaryKey = "";
                string primaryKeyCamel = "";
                string parameterPK = "";

                //string FK = "";
                //string FKIdFixed = ""; string FKCamel = "";


                //if (!string.IsNullOrEmpty(item.Cells[1].Value.ToString()))
                //{
                //    FKIdFixed = FK.Replace("ID", "Id");
                //    FKCamel = FKIdFixed.First().ToString().ToUpper() + String.Join("", FKIdFixed.Skip(1));
                //}

                int i = 0;
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {

                    columns[i, 0] = myField["ColumnName"].ToString();
                    columns[i, 1] = myField["DataType"].ToString();
                    columns[i, 2] = myField["ColumnSize"].ToString();
                    columns[i, 3] = myField["AllowDBNull"].ToString();

                    string columnName = myField["ColumnName"].ToString();

                    if ((columnName.Equals("createdDate") && !createdDate.Checked) ||
                        (columnName.Equals("modifiedDate") && !modifiedDate.Checked) ||
                        (columnName.Equals("createdBy") && !createdBy.Checked) ||
                        (columnName.Equals("modifiedBy") && !modifiedBy.Checked) ||
                        (columnName.Equals("rowVersion") && !rowVersion.Checked)
                        )
                    {
                        continue;
                    }


                    string columnNameCamel = columnName.First().ToString().ToUpper() + String.Join("", columnName.Skip(1));
                    columnNameCamel = columnNameCamel.Replace("ID", "Id");

                    string displayName = Regex.Replace(columnNameCamel, "(\\B[A-Z])", " $1");
                    string dataType = myField["DataType"].ToString().Replace("System.", "").Replace("Int16", "short").Replace("Int32", "int").Replace("Int", "int").Replace("String", "string").Replace("Boolean", "bool");
                    int size = Convert.ToInt32(myField["ColumnSize"]);
                    bool isNullable = Convert.ToBoolean(myField["AllowDBNull"]);
                    bool IsKey = Convert.ToBoolean(myField["IsKey"]);

                    if (IsKey)
                    {
                        primaryKey = columnName;
                        parameterPK = columnName.Replace("ID", "Id");
                        primaryKeyCamel = columnName.First().ToString().ToUpper() + String.Join("", columnName.Skip(1));
                        primaryKeyCamel = columnNameCamel.Replace("ID", "Id");
                    }

                    i++;
                }
                #region GetTableName

                repoCode = repoCode + "public async Task<" + viewModelName + "> Get" + tableName + " (int " + parameterPK + ")" + n + "{";
                repoCode = repoCode + "var result = await _db." + tableName + ".SingleOrDefaultAsync(e => e." + primaryKey + " == " +
                       parameterPK + ");" + n;

                repoCode = repoCode + @"if (result != null)
                    return " + viewModelName + @".ToModel(result);" + @"
           
                    Log.Warn(""" + tableName + @" is not found id=: "" + " + parameterPK + @");
                    return null;
                }";

                #endregion

                #region GetTableNameList

                repoCode = repoCode + @"
                    public List<" + viewModelName + @"> Get" + tableNameplural + @"List()
                    {
                        return _db." + tableName + @".Select(" + viewModelName + @".ToModel).ToList();
                    }";
                #endregion
                myReader.Close();

                #region GetTableNameByForeignKey
                FK(tableName);
                PK(tableName);
                foreach (DataGridViewRow item_ in FKGV.Rows)
                {
                    if (item_.Cells[0].Value == null)
                    {
                        continue;
                    }

                    string FKColumn = "";
                    string FKIdFixed = ""; string FKCamel = "";


                    if (item_.Cells[3].Value != null)
                    {
                        FKColumn = item_.Cells[3].Value.ToString();
                        FKIdFixed = FKColumn.Replace("ID", "Id");
                        FKCamel = FKIdFixed.First().ToString().ToUpper() + String.Join("", FKIdFixed.Skip(1));
                    }

                    repoCode = repoCode + "public List<" + viewModelName + "> Get" + tableNameplural + "By" + FKCamel + "(int " +
                           FKIdFixed + ")" + n + "{";
                    repoCode = repoCode + " var result = _db." + tableName + ".Where(a => a." + FKColumn + " == " + FKIdFixed +
                           ");" + n;
                    repoCode = repoCode + @" return result.Select(" + viewModelName + ".ToModel).ToList();" + n + "}" + n + n;
                }

                #endregion

                #region AddTableNameAsync

                repoCode = repoCode + @"

            public async Task<bool> Add" + tableName + @"Async(" + viewModelName + @" model)
            {
            try
            {
                _db." + tableName + @".Add(model.ToEntity());
                await _db.SaveChangesAsync();
                Log.Info(""" + tableName + @" added with id  "" + model." + primaryKeyCamel + @");
                return true;
            }
            catch (DbEntityValidationException ex)
            {
                var errors = new List<string>();
                foreach (var eve in ex.EntityValidationErrors)
                {
                    errors.Add(""Entity of type "" + eve.Entry.Entity.GetType().Name + "" in state "" + eve.Entry.State + "" has the following validation errors:"");
                    errors.AddRange(eve.ValidationErrors.Select(ve => "" Property: '"" + ve.PropertyName + ""'""+ "" Error: '"" + ve.ErrorMessage + ""'""));
                }

                Log.Error(""Failed to add " + tableName + @" with id  "" + model." + primaryKeyCamel + @" + string.Join("","", errors.ToArray()));
                return false;

            }
            catch (Exception ex)
            {
                Log.Error(""Failed to add " + tableName + @" with id  "" + model." + primaryKeyCamel + @", ex);
                return false;
            }" + n + "}" + n + n;

                #endregion

                #region UpdateTableNameAsync

                repoCode = repoCode + @" public async Task<bool> Update" + tableName + @"Async(" + viewModelName + @" model)
        {
            try
            {
                var entity = await _db." + tableName + @".SingleOrDefaultAsync(e => e." + primaryKey + @" == model." + primaryKeyCamel + @");
                entity = model.ToEntity(entity);
                _db.Entry(entity).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                Log.Info(""" + tableName + @" updated with id "" + model." + primaryKeyCamel + @");
                return true;
            }
            catch (DbEntityValidationException ex)
            {
                var errors = new List<string>();
                foreach (var eve in ex.EntityValidationErrors)
                {
                    errors.Add(""Entity of type "" + eve.Entry.Entity.GetType().Name + "" in state "" + eve.Entry.State + "" has the following validation errors:"");
                    errors.AddRange(eve.ValidationErrors.Select(ve => "" Property: '"" + ve.PropertyName + ""'""+ "" Error: '"" + ve.ErrorMessage + ""'""));
                }

                Log.Error(""Failed to update " + tableName + @" with id "" + model." + primaryKeyCamel + @" + "" - "" + string.Join("","", errors.ToArray()));
                return false;

            }
            catch (Exception ex)
            {
                Log.Error(""Failed to update " + tableName + @" with id "" + model." + primaryKeyCamel + @", ex);
                return false;
            }
        }" + n + n;

                #endregion



                #region DeleteTableNameAsync

                repoCode = repoCode + @"public async Task<bool> Delete" + tableName + @"Async(int id)
        {
            try
            {
                var result = await _db." + tableName + @".SingleOrDefaultAsync(e => e." + primaryKey + @" == id);
                if (result != null)
                {
                    _db." + tableName + @".Remove(result);
                    await _db.SaveChangesAsync();

                    Log.Info(""" + tableName + @" deleted with id "" + id);
                    return true;
                }
                Log.Info(""" + tableName + @" not found with id "" + id);
                return false;
                }
            catch (DbEntityValidationException ex)
            {
                var errors = new List<string>();
                foreach (var eve in ex.EntityValidationErrors)
                {
                    errors.Add(""Entity of type "" + eve.Entry.Entity.GetType().Name + "" in state "" + eve.Entry.State + "" has the following validation errors:"");
                    errors.AddRange(eve.ValidationErrors.Select(ve => "" Property: '"" + ve.PropertyName + ""'""+ "" Error: '"" + ve.ErrorMessage + ""'""));
                }

                Log.Error(""Failed to delete " + tableName + @" with id "" + id + "" - "" + string.Join("","", errors.ToArray()));
                return false;

            }
                catch (Exception ex)
                {
                    Log.Error(""Failed to delete " + tableName + @" with id "" + id, ex);
                    return false;
                }
              }
            " + n + n;

                #endregion

                repoCode = repoCode + "#endregion" + n + n;
            }
            RepoCodeText.Text = repoCode;
            RepoCodeText.Focus();
            RepoCodeText.SelectAll();
            ActionGen_Click();
        }

        private void TableNameRepo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TableNameRepo.SelectedItem != null)
            {
                RegionTB.Text = TableNameRepo.SelectedItem.ToString();
            }
        }

        private void TableCB_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void TableNameRepo_Click(object sender, EventArgs e)
        {
        }

        private void QueueBtn_Click(object sender, EventArgs e)
        {
            if (TableNameRepo.SelectedIndex == -1)
            {
                MessageBox.Show("Are you kidding me! Select a table first then click this button again!");
            }
            else if (TableGrid.Rows.Count > 1)
            {

                if (!TableGrid.Rows
                  .Cast<DataGridViewRow>()
                  .Any(r => r.Cells["CTableName"].Value != null && r.Cells["CTableName"].Value.ToString().Equals(TableNameRepo.Text)))
                    TableGrid.Rows.Add(TableNameRepo.Text, RegionTB.Text, RepoNameTB.Text, ControllerNameTB.Text, isPartial.Checked);

            }
            else
                TableGrid.Rows.Add(TableNameRepo.Text, RegionTB.Text, RepoNameTB.Text, ControllerNameTB.Text, isPartial.Checked);
            //foreach (var item in TableNameRepo.Items)
            //{
            //    TableGrid.Rows.Add(item.ToString(), FKTB.Text, item.ToString());
            //}


        }

        private void FK(string tableName)
        {

            SqlCommand cmd = new SqlCommand();
            DataSet ds = new DataSet("TimeRanges");

            cmd.CommandText = "sp_fkeys";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@fktable_name", SqlDbType.VarChar, 100).Value = tableName;
            cmd.Connection = _dbConnection;

            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = cmd;

            da.Fill(ds);

            FKGV.AutoGenerateColumns = true;
            FKGV.DataSource = ds;
            FKGV.DataMember = ds.Tables[0].TableName;
            // Data is accessible through the DataReader object here.
        }

        private void PK(string tableName)
        {

            SqlCommand cmd = new SqlCommand();
            DataSet ds = new DataSet("TimeRanges");

            cmd.CommandText = "sp_fkeys";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("@pktable_name", SqlDbType.VarChar, 100).Value = tableName;
            cmd.Connection = _dbConnection;

            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = cmd;

            da.Fill(ds);

            PKGV.AutoGenerateColumns = true;
            PKGV.DataSource = ds;
            PKGV.DataMember = ds.Tables[0].TableName;


        }


        public void ActionGen_Click()
        {

            SqlCommand cmd = new SqlCommand();

            string contCode = "";
            foreach (DataGridViewRow item in TableGrid.Rows)
            {
                if (item.Cells[0].Value == null)
                {
                    continue;
                }

                string tableName = item.Cells[0].Value.ToString();
                string repoName = item.Cells[2].Value.ToString();
                string controllerName = item.Cells[3].Value.ToString();
                bool isPartial = Convert.ToBoolean(item.Cells[4].Value);
                string viewModelName = tableName + "VM";

                string tableNameplural = ToPlural(tableName);
                //Retrieve records from the Employees table into a DataReader.
                cmd.Connection = _dbConnection;
                cmd.CommandText = "SELECT top 1 * FROM " + tableName;
                SqlDataReader myReader = cmd.ExecuteReader(CommandBehavior.KeyInfo);
                //Retrieve column schema into a DataTable.
                DataTable schemaTable = myReader.GetSchemaTable();

                string[,] columns = new string[100, 4];
                const string n = "\n";

                contCode = contCode + " #region " + tableName + n + n;

                string primaryKey = "";
                string primaryKeyCamel = "";
                string parameterPK = "";

                //string FK = "";
                //string FKIdFixed = ""; string FKCamel = "";


                //if (!string.IsNullOrEmpty(item.Cells[1].Value.ToString()))
                //{
                //    FKIdFixed = FK.Replace("ID", "Id");
                //    FKCamel = FKIdFixed.First().ToString().ToUpper() + String.Join("", FKIdFixed.Skip(1));
                //}

                int i = 0;
                //For each field in the table...
                foreach (DataRow myField in schemaTable.Rows)
                {

                    columns[i, 0] = myField["ColumnName"].ToString();
                    columns[i, 1] = myField["DataType"].ToString();
                    columns[i, 2] = myField["ColumnSize"].ToString();
                    columns[i, 3] = myField["AllowDBNull"].ToString();

                    string columnName = myField["ColumnName"].ToString();

                    if ((columnName.Equals("createdDate") && !createdDate.Checked) ||
                        (columnName.Equals("modifiedDate") && !modifiedDate.Checked) ||
                        (columnName.Equals("createdBy") && !createdBy.Checked) ||
                        (columnName.Equals("modifiedBy") && !modifiedBy.Checked) ||
                        (columnName.Equals("rowVersion") && !rowVersion.Checked)
                        )
                    {
                        continue;
                    }


                    string columnNameCamel = columnName.First().ToString().ToUpper() + String.Join("", columnName.Skip(1));
                    columnNameCamel = columnNameCamel.Replace("ID", "Id");

                    string displayName = Regex.Replace(columnNameCamel, "(\\B[A-Z])", " $1");
                    string dataType = myField["DataType"].ToString().Replace("System.", "").Replace("Int16", "short").Replace("Int32", "int").Replace("Int", "int").Replace("String", "string").Replace("Boolean", "bool");
                    int size = Convert.ToInt32(myField["ColumnSize"]);
                    bool isNullable = Convert.ToBoolean(myField["AllowDBNull"]);
                    bool IsKey = Convert.ToBoolean(myField["IsKey"]);

                    if (IsKey)
                    {
                        primaryKey = columnName;
                        parameterPK = columnName.Replace("ID", "Id");
                        primaryKeyCamel = columnName.First().ToString().ToUpper() + String.Join("", columnName.Skip(1));
                        primaryKeyCamel = columnNameCamel.Replace("ID", "Id");
                    }

                    i++;
                }
                if (!isPartial)
                {

                    #region TableNameDetails

                    string actionName = tableName + @"Details";
                    contCode = contCode + @"
                // GET: /" + controllerName + "/" + actionName + @"/5
                public async Task<ActionResult> " + actionName + @"(int? id)
                {
                    if (id == null)
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

                    var model = await " + repoName + @".Get" + tableName + @"(id.Value);
                    if (model == null)
                    {
                        return HttpNotFound();
                    }
                    Log.Info(""" + actionName + @" is called id: "" + id);
                    return View(model);
                }" + n;

                    #endregion

                    #region AddTableName

                    contCode = contCode + @"
                // GET: /""" + controllerName + @"/Add" + tableName + @"
                public ActionResult Add" + tableName + @"()
                {
                    var model = new " + viewModelName + @"();
                    return View(model);
                }

                // POST:  /""" + controllerName + @"/Add" + tableName + @"
                    [HttpPost]
                public async Task<ActionResult> Add" + tableName + @"(" + viewModelName + @" model)
                {
                    if (ModelState.IsValid)
                    {
                        if (await " + repoName + @".Add" + tableName + @"Async(model))
                        {
                            ViewBag.SuccessMessage = """ + tableName + @" "" + model." + primaryKeyCamel + @" + "" added"";
                            return RedirectToAction(""List" + tableName + @""");
                        }
                        ModelState.AddModelError(""" + tableName + @"AddingError"", ""There was a problem adding the " + tableName + @" "" + model." + primaryKeyCamel + @");
                        return View(model);
                    }
                    return View(model);
                 }" + n + n;

                    #endregion

                    #region EditTableName

                    contCode = contCode + @" 
                // GET: /" + controllerName + @"/Edit" + tableName + @"
                public async Task<ActionResult> Edit" + tableName + @"(int? id)
                {
                    if (id == null)
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                  
                    var model = await " + repoName + @".Get" + tableName + @"(id.Value);
                    if (model!=null)
                    { 
                       return View(""Edit" + tableName + @""", model);
                    }
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                    // POST: /" + controllerName + @"/Edit" + tableName + @"
                    [HttpPost]
                    public async Task<ActionResult> Edit" + tableName + @"(" + viewModelName + @" model)
                    {
                        if (ModelState.IsValid)
                        {
                            if (await " + repoName + @".Update" + tableName + @"Async(model))
                            {
                                ViewBag.SuccessMessage = """ + tableName + @" "" + model." + primaryKeyCamel + @" + "" saved"";
                                return RedirectToAction(""List" + tableNameplural + @""");
                            }
                            ModelState.AddModelError(""" + tableName + @"EditError"", ""There was a problem editing the " + tableName + @" "" + model." + primaryKeyCamel + @");
                            return View(model);
                        }
                        return View(model);

                    }" + n;
                    #endregion

                    #region ListTableName

                    contCode = contCode + @"  
                // GET: /" + controllerName + @"/List" + tableNameplural + @"
                public ActionResult List" + tableNameplural + @"()
                {
                    //Log.Info(""" + tableName + @" called"");
                    return View(" + repoName + @".Get" + tableNameplural + @"List());
                }" + n + n;
                    #endregion

                    #region DeleteTableName

                    contCode = contCode + @" 
                // GET: /" + controllerName + @"/Delete" + tableName + @"/5
                public async Task<ActionResult> Delete" + tableName + @"(int? id)
                {
                    if (id == null)
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                    
                    var model = await " + repoName + @".Get" + tableName + @"(id.Value);
                    if (model == null)
                    {
                     return new HttpStatusCodeResult(HttpStatusCode.NotFound);
                    }
                    Log.Info(""" + actionName + @" is called id: "" + id);
                    return View(model);
                }
                " + n;

                    contCode = contCode + @"   
                // POST: /" + controllerName + @"/Delete" + tableName + @"/5
                [HttpPost, ActionName(""Delete" + tableName + @""")]
                public async Task<ActionResult> DeleteConfirmed" + tableName + @"(int id)
                {
                    //var model = await " + repoName + @".Get" + tableName + @"(id);
                    await " + repoName + @".Delete" + tableName + @"Async(id);
                    return RedirectToAction(""List" + tableNameplural + @""");
                }" + n + n;
                    #endregion

                    #region GetTableNameByForeignKey
                    myReader.Close();
                    FK(tableName);
                    PK(tableName);
                    foreach (DataGridViewRow item_ in FKGV.Rows)
                    {
                        if (item_.Cells[0].Value == null)
                        {
                            continue;
                        }

                        string FKCamel = "";


                        if (item_.Cells[3].Value != null)
                        {
                            string FKColumn = item_.Cells[3].Value.ToString();
                            string FKIdFixed = FKColumn.Replace("ID", "Id");
                            FKCamel = FKIdFixed.First().ToString().ToUpper() + String.Join("", FKIdFixed.Skip(1));
                        }

                        contCode = contCode + @"
                            // GET: /" + controllerName + @"/" + tableNameplural + @"By" + FKCamel + @"
                            public ActionResult " + tableNameplural + @"By" + FKCamel + @"(int? id)
                            {
                                if (id == null)
                                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                                
                                Log.Info(""" + tableNameplural + @"By" + FKCamel + @" called with id: ""+id.Value);
                                return View(" + repoName + @".Get" + tableNameplural + "By" + FKCamel + @"(id.Value));
                            }" + n;


                        //code = code + "public List<" + viewModelName + "> Get" + tableNameplural + "By" + FKCamel + "(int " +
                        //       FKIdFixed + ")" + n + "{";
                        //code = code + " var result = _db." + tableName + ".Where(a => a." + FKColumn + " == " + FKIdFixed +
                        //       ");" + n;
                        //code = code + @" return result.Select(" + viewModelName + ".ToModel).ToList();" + n + "}" + n + n;
                    }

                    #endregion


                }
                else
                {

                    #region TableNameDetails

                    string actionName = tableName + @"Details";
                    contCode = contCode + @"
                // GET: /" + controllerName + "/" + actionName + @"/5
                public async Task<ActionResult> " + actionName + @"(int? id)
                {
                    if (id == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                    }
                    var model = await " + repoName + @".Get" + tableName + @"(id.Value);
                    if (model == null)
                    {
                        Log.Warn(""" + tableName + @" is not found id=: "" + id);
                        return HttpNotFound();
                    }
                    Log.Info(""" + actionName + @" is called id: "" + id);
                    return PartialView(""_" + actionName + @""", model);

                }" + n + n;

                    #endregion

                    #region AddTableName

                    contCode = contCode + @"
                // GET: /""" + controllerName + @"/Add" + tableName + @"
                public ActionResult Add" + tableName + @"()
                {
                    var model = new " + viewModelName + @"();
                   return PartialView(""_Add" + tableName + @""", model);
                }

                // POST:  /""" + controllerName + @"/Add" + tableName + @"
                    [HttpPost]
                public async Task<ActionResult> Add" + tableName + @"(" + viewModelName + @" model)
                {
                    if (ModelState.IsValid)
                    {
                        if (await " + repoName + @".Add" + tableName + @"Async(model))
                        {
                            ViewBag.SuccessMessage = """ + tableName + @" "" + model." + primaryKeyCamel + @" + "" added"";
                            return RedirectToAction(""List" + tableName + @""");
                        }
                        ModelState.AddModelError(""" + tableName + @"AddingError"", ""There was a problem adding the " + tableName + @" "" + model." + primaryKeyCamel + @");
                   return PartialView(""_Add" + tableName + @""", model);
                    }
                   return PartialView(""_Add" + tableName + @""", model);
                 }" + n + n;

                    #endregion

                    #region EditTableName

                    contCode = contCode + @" 
                // GET: /" + controllerName + @"/Edit" + tableName + @"
                public async Task<ActionResult> Edit" + tableName + @"(int? id)
                {
                    var model = new " + viewModelName + @"();
                    //get the " + tableName + @" from the DB
                    if (id.HasValue)
                    {
                        model = await " + repoName + @".Get" + tableName + @"(id.Value);
                    }
                    return PartialView(""_Edit" + tableName + @""", model);
                }


                    // POST: /" + controllerName + @"/Edit" + tableName + @"
                    [HttpPost]
                    public async Task<ActionResult> Edit" + tableName + @"(" + viewModelName + @" model)
                    {
                        if (ModelState.IsValid)
                        {
                            if (await " + repoName + @".Update" + tableName + @"Async(model))
                            {
                                ViewBag.SuccessMessage = """ + tableName + @" "" + model." + primaryKeyCamel + @" + "" saved"";
                                return RedirectToAction(""List" + tableNameplural + @""");
                            }
                            ModelState.AddModelError(""" + tableName + @"EditError"", ""There was a problem editing the " + tableName + @" "" + model." + primaryKeyCamel + @");
                    return PartialView(""_Edit" + tableName + @""", model);
                        }
                    return PartialView(""_Edit" + tableName + @""", model);

                    }" + n + n;
                    #endregion

                    #region ListTableName

                    contCode = contCode + @"  
                // GET: /" + controllerName + @"/List" + tableNameplural + @"
                public ActionResult List" + tableNameplural + @"()
                {
                    Log.Info(""" + tableName + @" called"");
                    return PartialView(""_Edit" + tableName + @""", " + repoName + @".Get" + tableNameplural + @"List());
                }" + n + n;
                    #endregion

                    #region DeleteTableName

                    contCode = contCode + @" 
                // GET: /" + controllerName + @"/Delete" + tableName + @"/5
                public async Task<ActionResult> Delete" + tableName + @"(int? id)
                {
                    if (id == null)
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                    }
                    var model = await " + repoName + @".Get" + tableName + @"(id.Value);
                    if (model == null)
                    {
                        Log.Warn(""" + tableName + @" is not found id=: "" + id);
                        return HttpNotFound();
                    }
                    Log.Info(""" + actionName + @" is called id: "" + id);
                    return PartialView(""_Delete" + tableName + @""", model);

                }
                " + n;

                    contCode = contCode + @"   
                // POST: /" + controllerName + @"/Delete" + tableName + @"/5
                [HttpPost, ActionName(""Delete" + tableName + @""")]
                public async Task<ActionResult> DeleteConfirmed" + tableName + @"(int id)
                {
                    var model = await " + repoName + @".Get" + tableName + @"(id);
                    await Task.Run(() =>
                    {
                        " + repoName + @".Delete" + tableName + @"Async(id);
                    });
                    return RedirectToAction(""List" + tableNameplural + @""");
                }" + n + n;
                    #endregion

                    #region GetTableNameByForeignKey

                    FK(tableName);
                    PK(tableName);
                    foreach (DataGridViewRow item_ in FKGV.Rows)
                    {
                        if (item_.Cells[0].Value == null)
                        {
                            continue;
                        }

                        string FKCamel = "";


                        if (item_.Cells[3].Value != null)
                        {
                            string FKColumn = item_.Cells[3].Value.ToString();
                            string FKIdFixed = FKColumn.Replace("ID", "Id");
                            FKCamel = FKIdFixed.First().ToString().ToUpper() + String.Join("", FKIdFixed.Skip(1));
                        }

                        contCode = contCode + @"
                            // GET: /" + controllerName + @"/" + tableNameplural + @"By" + FKCamel + @"
                            public ActionResult " + tableNameplural + @"By" + FKCamel + @"(int? id)
                            {
                                if (id == null)
                                {
                                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                                }
                                Log.Info(""" + tableNameplural + @"By" + FKCamel + @" called with id: ""+id.Value);
                                return View(" + repoName + @".Get" + tableNameplural + "By" + FKCamel + @"(id.Value));
                            }" + n + n;


                        //code = code + "public List<" + viewModelName + "> Get" + tableNameplural + "By" + FKCamel + "(int " +
                        //       FKIdFixed + ")" + n + "{";
                        //code = code + " var result = _db." + tableName + ".Where(a => a." + FKColumn + " == " + FKIdFixed +
                        //       ");" + n;
                        //code = code + @" return result.Select(" + viewModelName + ".ToModel).ToList();" + n + "}" + n + n;
                    }

                    #endregion
                }

                #region JSON list

                contCode = contCode + @"
                #region Json
  
                // Json GET: /" + controllerName + @"/List" + tableNameplural + @"
                public ActionResult List" + tableNameplural + @"Json()
                {
                    //Log.Info(""" + tableName + @" called"");
                    return Json(" + repoName + @".Get" + tableNameplural + @"List(), JsonRequestBehavior.AllowGet);
                }" + n + n;


                foreach (DataGridViewRow item_ in FKGV.Rows)
                {
                    if (item_.Cells[0].Value == null)
                    {
                        continue;
                    }

                    var FKIdFixed = ""; string FKCamel = "";


                    if (item_.Cells[3].Value != null)
                    {
                        string FKColumn = item_.Cells[3].Value.ToString();
                        FKIdFixed = FKColumn.Replace("ID", "Id");
                        FKCamel = FKIdFixed.First().ToString().ToUpper() + String.Join("", FKIdFixed.Skip(1));
                    }

                    contCode = contCode + @"
                            // Json GET: /" + controllerName + @"/" + tableNameplural + @"By" + FKCamel + @"Json
                            public ActionResult " + tableNameplural + @"By" + FKCamel + @"Json(int? id)
                            {
                                if (id == null)
                                {
                                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                                }
                                return Json(" + repoName + @".Get" + tableNameplural + "By" + FKCamel + @"(id.Value), JsonRequestBehavior.AllowGet);
                            }" + n + n;
                }

                contCode = contCode + @"
                #endregion";
                #endregion

                #region Kendo Grid

                contCode = contCode + @"
                
                #region Kendo Grid
      
                // Json GET: /" + controllerName + @"/List" + tableNameplural + @"
                public ActionResult List" + tableNameplural + @"Kendo([DataSourceRequest] DataSourceRequest request)
                {
                    //Log.Info(""" + tableName + @" called"");
                    return Json(" + repoName + @".Get" + tableNameplural + @"List().ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
                }" + n + n;


                foreach (DataGridViewRow item_ in FKGV.Rows)
                {
                    if (item_.Cells[0].Value == null)
                    {
                        continue;
                    }

                    string FKCamel = "";


                    if (item_.Cells[3].Value != null)
                    {
                        string FKColumn = item_.Cells[3].Value.ToString();
                        string FKIdFixed = FKColumn.Replace("ID", "Id");
                        FKCamel = FKIdFixed.First().ToString().ToUpper() + String.Join("", FKIdFixed.Skip(1));
                    }

                    contCode = contCode + @"
                            // Json GET: /" + controllerName + @"/" + tableNameplural + @"By" + FKCamel + @"Json
                            public ActionResult " + tableNameplural + @"By" + FKCamel + @"Kendo(int? id,[DataSourceRequest] DataSourceRequest request)
                            {
                                if (id == null)
                                {
                                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                                }
                                return Json(" + repoName + @".Get" + tableNameplural + "By" + FKCamel + @"(id.Value).ToDataSourceResult(request), JsonRequestBehavior.AllowGet);
                            }" + n + n;
                }
                contCode = contCode + @"
                #endregion" + n + n;

                #endregion
                contCode = contCode + "#endregion" + n + n;

            }
            //Always close the DataReader and connection.
            ActionsResult.Text = contCode;
            ActionsResult.Focus();
            ActionsResult.SelectAll();
        }


        public string ToPlural(string text)
        {

            string tableNameplural = text + "s";
            if (text.Last().Equals('y'))
            {
                //agency agenc ies
                tableNameplural = text.Remove(text.Length - 1) + "ies";
            }
            if (tableNameplural.Substring(tableNameplural.Length - 2).Equals("ss"))
            {
                tableNameplural = tableNameplural.Remove(tableNameplural.Length - 1);
            }

            if (tableNameplural.Substring(tableNameplural.Length - 3).Equals("sss"))
            {
                tableNameplural = tableNameplural.Remove(tableNameplural.Length - 2) + "es";
            }
            return tableNameplural;
        }

        private void RepoCodeText_Click(object sender, EventArgs e)
        {
            RepoCodeText.SelectAll();
            if (!string.IsNullOrEmpty(RepoCodeText.Text))
                Clipboard.SetText(RepoCodeText.Text);
        }

        private void ActionsResult_Click(object sender, EventArgs e)
        {
            ActionsResult.SelectAll();
            if (!string.IsNullOrEmpty(ActionsResult.Text))
                Clipboard.SetText(ActionsResult.Text);

        }

        private void DBSettings_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ServeTB.Text.Length > 0 && UserTB.Text.Length > 0 && PassTB.Text.Length > 0)
            {
                TableNameRepo.Items.Clear();

                List<string> tables = new List<string>();

                DataTable dt = _dbConnection.GetSchema("Tables");
                foreach (DataRow row in dt.Rows)
                {
                    if (((string)row[3]).Equals("BASE TABLE") && ((string)row[1]).Equals("dbo"))
                    {
                        string tablename = (string)row[2];
                        tables.Add(tablename);
                    }
                }
                tables.Sort();
                foreach (var item in tables)
                {
                    TableNameRepo.Items.Add(item);
                }
            }
        }

        private void ServeTB_TextChanged(object sender, EventArgs e)
        {

        }

        private void ViewModelGen_FormClosing(object sender, FormClosingEventArgs e)
        {
            _dbConnection.Close();
        }

        private void DisplayAnn_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void RepoNameTB_TextChanged(object sender, EventArgs e)
        {

        }

        private void TableNameRepo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                QueueBtn.PerformClick();
            }
        }

        private void TableCB_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                VMGenB.PerformClick();
            }
        }

        private void CodeText_Click(object sender, EventArgs e)
        {
            CodeText.SelectAll();
            if (!string.IsNullOrEmpty(CodeText.Text))
                Clipboard.SetText(CodeText.Text);
        }

    }
}