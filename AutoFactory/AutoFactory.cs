/*
/*
 --------------------------------------------
 - AutoFactory created by Daniel Dollerup   -
 -                                          -
 - You are free to use this as you please   -
 - as long as you credit me :)              -
 -                                          -
 - Latest Update: 22-10-2018                -
 --------------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;
using System.Reflection;
using System.Dynamic;
using System.Security.Cryptography;

public enum WeekdayEN
{
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Sunday
}

public enum SearchSpecifier
{
    Specific,
    Unspecific
}
public class AutoFactory<T>
{
    // Local reference to the ConnectionString set in the WebConfig Root file.
    private string connectionString = "";

    // Creating a List of Properties, containing information about the current Type's properties
    private List<PropertyInfo> properties = new List<PropertyInfo>();

    /// <summary>
    /// Constructor for AutoFactory.
    /// </summary>
    public AutoFactory()
    {
        #region .NET
        //Get the ConnectionString from the WebConfig
        connectionString = ConfigurationManager.ConnectionStrings["String"].ConnectionString;
        #endregion

        #region .NET CORE
        //IConfiguration configuration = default(IConfiguration);
        //var builder = new ConfigurationBuilder()
        //    .SetBasePath(Directory.GetCurrentDirectory())
        //    .AddJsonFile("appsettings.json");
        //configuration = builder.Build();
        //connectionString = configuration["ConnectionStrings:DefaultConnection"];
        #endregion

        properties.AddRange(GetGenericType().GetType().GetProperties());
    }

    /// <summary>
    /// Used to create a temp instance of the current Type
    /// </summary>
    /// <returns>The temp instance of current Type</returns>
    private T GetGenericType()
    {
        T t;
        return t = Activator.CreateInstance<T>();
    }
    private Q GetGenericType<Q>()
    {
        Q t;
        return t = Activator.CreateInstance<Q>();
    }

    /// <summary>
    /// Generates a Token for the entity
    /// </summary>
    /// <param name="entity">entity to create from</param>
    /// <returns>Generated Token for this entity</returns>
    private void GenerateToken(T entity)
    {
        PropertyInfo tokenProp = entity.GetType().GetProperties().FirstOrDefault(x => x.Name.ToLower() == "token");
        if (tokenProp != null)
        {
            string sqlQuery = string.Format("SELECT IDENT_CURRENT ('{0}')", typeof(T).Name);
            SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            SqlCommand cmd = new SqlCommand(sqlQuery, connection);
            int nextID = Convert.ToInt32(cmd.ExecuteScalar()) + (Count() == 0 ? 0 : 1);

            cmd.Dispose();
            connection.Dispose();
            connection.Close();

            SHA512 tokenHash = new SHA512Managed();
            tokenHash.ComputeHash(System.Text.Encoding.ASCII.GetBytes(typeof(T).Name + nextID));
            tokenProp.SetValue(entity, BitConverter.ToString(tokenHash.Hash).Replace("-", "").ToLower());
        }
    }

    /// <summary>
    /// Generates a SHA512 Hash from string value
    /// </summary>
    /// <param name="from">value to create from</param>
    /// <returns></returns>
    public string GenerateSHA512Hash(string from)
    {
        SHA512 tokenHash = new SHA512Managed();
        tokenHash.ComputeHash(System.Text.Encoding.ASCII.GetBytes(from));
        return BitConverter.ToString(tokenHash.Hash).Replace("-", "").ToLower();
    }

    /// <summary>
    /// Adds entity to the database table
    /// </summary>
    /// <param name="entity">Entity to Add</param>
    /// <returns>The entity that has been added. Inludes ID and Token</returns>
    public T Insert(T entity)
    {
        string sqlQuery = "";

        if (properties.Count > 1)
        {
            // Generate Token
            GenerateToken(entity);

            // Sql insert query - {0} is the table name (typeof(T).Name)
            sqlQuery = string.Format("INSERT INTO [{0}] (", typeof(T).Name);

            // Loops through the properties of the current Type
            for (int i = 0; i < properties.Count; i++)
            {
                // For each property, we add the property name to the sql query
                // where the property name refers to a column in the table
                PropertyInfo property = properties[i];
                if (property.Name.ToLower().Contains("id") && i == 0) continue;

                sqlQuery += property.Name;
                sqlQuery += (i + 1 == properties.Count ? "" : ", ");
            }



            // Ending the Insert statement
            sqlQuery += ") ";
            // Starting the Values statement
            sqlQuery += "VALUES (";

            // Loops through the properties of the current Type
            for (int i = 0; i < properties.Count; i++)
            {
                // We add parameterized queries to the sql query
                // A parameterized query starts with @ and then the column name
                PropertyInfo property = properties[i];
                if (property.Name.ToLower().Contains("id") && i == 0) continue;

                sqlQuery += "@" + property.Name + (i + 1 == properties.Count ? "" : ", ");
            }

            // Ending the Values statement
            sqlQuery += ")";
        }
        else
        {
            sqlQuery = string.Format("INSERT INTO [{0}] DEFAULT VALUES", typeof(T).Name);
        }

        sqlQuery += "; SELECT SCOPE_IDENTITY()";

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Loops through the properties of the current Type
        for (int i = 0; i < properties.Count; i++)
        {
            // Passing in values for the properties
            if (properties[i].Name.ToLower().Contains("id") && i == 0) continue;
            object prop = properties[i].GetValue(entity);
            if (prop?.GetType() == typeof(DateTime) && ((DateTime)prop) == DateTime.MinValue) prop = DBNull.Value;
            cmd.Parameters.AddWithValue("@" + properties[i].Name, prop ?? DBNull.Value);
        }

        // Executing the SQL statement
        int id = Convert.ToInt32(cmd.ExecuteScalar());


        // Disposing and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();
        return Get(id);
    }

    /// <summary>
    /// Update entity
    /// </summary>
    /// <param name="entity">The entity to update, must contain ID</param>
    public void Update(T entity)
    {
        string sqlQuery = string.Format("UPDATE [{0}] SET ", typeof(T).Name);

        // Loops through the properties of the current Type
        for (int i = 0; i < properties.Count; i++)
        {
            // For each property, we add the property name to the sql query
            // where the property name refers to a column in the table
            PropertyInfo property = properties[i];
            if (property.Name.ToLower().Contains("id") && i == 0) continue;

            sqlQuery += property.Name + "=@" + property.Name;
            sqlQuery += (i + 1 == properties.Count ? "" : ", ");
        }

        // Adding Filtering with WHERE statement
        sqlQuery += string.Format(" WHERE ID = '{0}' ", properties[0].GetValue(entity));

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Loops through the properties of the current Type
        for (int i = 0; i < properties.Count; i++)
        {
            // Passing in values for the properties
            if (properties[i].Name.ToLower().Contains("id") && i == 0) continue;
            object prop = properties[i].GetValue(entity);
            if (prop?.GetType() == typeof(DateTime) && ((DateTime)prop) == DateTime.MinValue) prop = DBNull.Value;
            cmd.Parameters.AddWithValue("@" + properties[i].Name, prop ?? DBNull.Value);
        }

        // Executing the SQL statement
        cmd.ExecuteNonQuery();

        // Disposing and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();
    }

    /// <summary>
    /// Get entity from the database
    /// </summary>
    /// <param name="id"></param>
    /// <returns>The requested Entity by ID filtering</returns>
    public T Get(int id)
    {
        // Creating the SELECT SQL Statement, with {0} as Table name and {1} as the ID
        string sqlQuery = string.Format("SELECT * FROM [{0}] WHERE ID = '{1}'", typeof(T).Name, id);

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the response from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a result object to hold the response from the database
        T result = GetGenericType();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(result, reader[i]);
                }
            }
        }

        // Disposing and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Get entity from the database
    /// </summary>
    /// <param name="field">field in table</param>
    /// <param name="value">value to compare with</param>
    /// <returns>The requested Entity by field and value filtering</returns>
    public T Get(string field, object value)
    {
        // Creating the SELECT SQL Statement, with {0} as Table name and {1} as the ID
        string sqlQuery = string.Format("SELECT * FROM [{0}] WHERE {1} = '{2}'", typeof(T).Name, field, value);

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the response from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a result object to hold the response from the database
        T result = GetGenericType();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(result, reader[i]);
                }
            }
        }

        // Disposing and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Gets all entities from the database
    /// </summary>
    /// <returns>Returns a list of T where T is a table from the database</returns>
    public List<T> GetAll()
    {
        // Creating the SELECT SQL Statement, with {0} as Table name
        string sqlQuery = string.Format("SELECT * FROM [{0}]", typeof(T).Name);

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the _response_ from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a entity holder object to hold the response from the database
        T entity = default(T);

        // Creating a result list to hold the _responses_ from the database
        List<T> result = new List<T>();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Creating the Entity object, it can now be used to set data
                entity = GetGenericType();
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(entity, reader[i], null);
                }
                // Adding the entity to the List and continuing to the next field
                result.Add(entity);
            }
        }

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Gets all elements from table by field and value
    /// </summary>
    /// <param name="field">Field in table</param>
    /// <param name="value">Value to compare</param>
    /// <returns>List of elements that matches specifics</returns>
    public List<T> GetAllBy(string field, object value)
    {
        // Creating the SELECT SQL Statement, with {0} as Table name, {1} as field to search in and {2} as the value to filter with
        string sqlQuery = string.Format("SELECT * FROM [{0}] WHERE [{1}] = '{2}'", typeof(T).Name, field, value);

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the _response_ from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a entity holder object to hold the response from the database
        T entity = default(T);

        // Creating a result list to hold the _responses_ from the database
        List<T> result = new List<T>();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Creating the Entity object, it can now be used to set data
                entity = GetGenericType();
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(entity, reader[i], null);
                }
                // Adding the entity to the List and continuing to the next field
                result.Add(entity);
            }
        }

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Gets all elements from table that matches requirements. Creates an InnerJoin that intersects on IDs between T and T1.
    /// </summary>
    /// <typeparam name="T1">Table to join</typeparam>
    /// <param name="value">Value to compare</param>
    /// <param name="keyReference">The intersection of tables you wish to filter by.</param>
    /// <returns>List of elements from table that matches the value and fields</returns>
    public List<T> GetAllByJoin<T1>(object keyReference, object value)
    {
        // Creating the SELECT SQL Statement, with {0} as Table name
        string sqlQuery = string.Format("SELECT [{0}].* FROM [{0}] ", typeof(T).Name);
        sqlQuery += string.Format("INNER JOIN [{0}] ON [{0}].{1}ID = [{1}].ID", typeof(T1).Name, typeof(T).Name);

        sqlQuery += string.Format(" WHERE [{0}]='{1}'", keyReference, value);

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the _response_ from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a entity holder object to hold the response from the database
        T entity = default(T);

        // Creating a result list to hold the _responses_ from the database
        List<T> result = new List<T>();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Creating the Entity object, it can now be used to set data
                entity = GetGenericType();
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(entity, reader[i], null);
                }
                // Adding the entity to the List and continuing to the next field
                result.Add(entity);
            }
        }

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Gets all elements from table that matches requirements.
    /// </summary>
    /// <param name="value">The search parameter</param>
    /// <param name="fields">The fields in the table you wish to include</param>
    /// <returns>List of elements from table that matches the value and fields</returns>
    public List<T> SearchBy(object value, params string[] fields)
    {
        // Creating the SELECT SQL Statement, with {0} as Table name
        string sqlQuery = string.Format("SELECT * FROM [{0}] WHERE (", typeof(T).Name);
        // Adding fields to the SQL statement
        for (int i = 0; i < fields.Length; i++)
        {
            sqlQuery += string.Format("([{0}] LIKE '%{1}%')", fields[i], value);
            if (i + 1 < fields.Length)
            {
                sqlQuery += " OR ";
            }
        }
        sqlQuery += ")";

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the _response_ from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a entity holder object to hold the response from the database
        T entity = default(T);

        // Creating a result list to hold the _responses_ from the database
        List<T> result = new List<T>();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Creating the Entity object, it can now be used to set data
                entity = GetGenericType();
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(entity, reader[i], null);
                }
                // Adding the entity to the List and continuing to the next field
                result.Add(entity);
            }
        }

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Gets all elements from table that matches requirements.
    /// </summary>
    /// <param name="value">The search parameter</param>
    /// <param name="filters">filters you wish to add to your search. Add them through a new string[] { "#FILTER#", "#FILTER#" } </param>
    /// <param name="searchSpecifier">Defines wether the search should add "AND" or "OR" between filters.</param>
    /// <param name="fields">The fields in the table you wish to include</param>
    /// <returns>List of elements from table that matches the value, filters and fields</returns>
    public List<T> SearchByFilter(object value, string[] filters, SearchSpecifier searchSpecifier = SearchSpecifier.Specific, params string[] fields)
    {
        // Creating the SELECT SQL Statement, with {0} as Table name
        string sqlQuery = string.Format("SELECT * FROM [{0}] WHERE (", typeof(T).Name);


        // Adding filters to the SQL Statement
        for (int i = 0; i < filters.Length; i++)
        {
            sqlQuery += "(" + filters[i] + ")";
            if (i + 1 < filters.Length)
            {
                sqlQuery += (searchSpecifier == SearchSpecifier.Specific ? " AND " : " OR ");
            }
        }

        if (fields.Length > 0)
        {
            sqlQuery += " AND (";

            // Adding fields to the SQL statement
            for (int i = 0; i < fields.Length; i++)
            {
                sqlQuery += string.Format("([{0}] LIKE '%{1}%')", fields[i], value);
                if (i + 1 < fields.Length)
                {
                    sqlQuery += " OR ";
                }
            }
            sqlQuery += ")";
        }

        sqlQuery += ")";

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the _response_ from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a entity holder object to hold the response from the database
        T entity = default(T);

        // Creating a result list to hold the _responses_ from the database
        List<T> result = new List<T>();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Creating the Entity object, it can now be used to set data
                entity = GetGenericType();
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(entity, reader[i], null);
                }
                // Adding the entity to the List and continuing to the next field
                result.Add(entity);
            }
        }

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Gets all elements from table that matches requirements.
    /// </summary>
    /// <typeparam name="T1">Table to join</typeparam>
    /// <param name="value">The search parameter</param>
    /// <param name="fields">The fields in the table you wish to include</param>
    /// <returns>List of elements from table that matches the value and fields</returns>
    public List<T> SearchByJoin<T1>(object value, params string[] fields)
    {
        // Creating the SELECT SQL Statement, with {0} as Table name
        string sqlQuery = string.Format("SELECT [{0}].* FROM [{0}] ", typeof(T).Name);
        sqlQuery += string.Format("INNER JOIN [{0}] ON [{1}].{0}ID = [{0}].ID", typeof(T1).Name, typeof(T).Name);

        sqlQuery += " WHERE ";

        // Adding fields to the SQL statement
        for (int i = 0; i < fields.Length; i++)
        {
            sqlQuery += string.Format("([{2}].{0} LIKE '%{1}%')", fields[i], value, typeof(T).Name);
            sqlQuery += " OR ";
        }

        #region QEntity
        List<PropertyInfo> propertiesFromJoin = new List<PropertyInfo>(GetGenericType<T1>().GetType().GetProperties());

        for (int i = 0; i < propertiesFromJoin.Count; i++)
        {
            PropertyInfo property = propertiesFromJoin[i];
            if (property.Name.ToLower().Contains("id") && i == 0) continue;
            sqlQuery += string.Format("[{0}].{1} LIKE '%" + value + "%'", typeof(T1).Name, property.Name);

            if (i + 1 < propertiesFromJoin.Count)
            {
                sqlQuery += " OR ";
            }
        }
        #endregion

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the _response_ from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a entity holder object to hold the response from the database
        T entity = default(T);

        // Creating a result list to hold the _responses_ from the database
        List<T> result = new List<T>();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Creating the Entity object, it can now be used to set data
                entity = GetGenericType();
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(entity, reader[i], null);
                }
                // Adding the entity to the List and continuing to the next field
                result.Add(entity);
            }
        }

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Gets all elements from table that matches requirements.
    /// </summary>
    /// <typeparam name="T1">First table to join</typeparam>
    /// <typeparam name="T2">Second table to join</typeparam>
    /// <param name="value">The search parameter</param>
    /// <param name="fields">The fields in the table you wish to include</param>
    /// <returns>List of elements from table that matches the value and fields</returns>
    public List<T> SearchByJoin<T1, T2>(object value, params string[] fields)
    {
        // Creating the SELECT SQL Statement, with {0} as Table name
        string sqlQuery = string.Format("SELECT [{0}].* FROM [{0}] ", typeof(T).Name);
        sqlQuery += string.Format("INNER JOIN [{0}] ON [{1}].{0}ID = [{0}].ID ", typeof(T1).Name, typeof(T).Name);
        sqlQuery += string.Format("INNER JOIN [{0}] ON [{1}].{0}ID = [{0}].ID ", typeof(T2).Name, typeof(T).Name);

        sqlQuery += "WHERE ";

        // Adding fields to the SQL statement
        for (int i = 0; i < fields.Length; i++)
        {
            sqlQuery += string.Format("([{2}].{0} LIKE '%{1}%')", fields[i], value, typeof(T).Name);
            sqlQuery += " OR ";
        }

        #region QEntity
        List<PropertyInfo> propertiesFromJoin = new List<PropertyInfo>(GetGenericType<T1>().GetType().GetProperties());

        for (int i = 0; i < propertiesFromJoin.Count; i++)
        {
            PropertyInfo property = propertiesFromJoin[i];
            if (property.Name.ToLower().Contains("id") && i == 0) continue;
            sqlQuery += string.Format("([{0}].{1} LIKE '%" + value + "%')", typeof(T1).Name, property.Name);

            sqlQuery += " OR ";
        }
        #endregion

        #region PEntity
        propertiesFromJoin = new List<PropertyInfo>(GetGenericType<T2>().GetType().GetProperties());

        for (int i = 0; i < propertiesFromJoin.Count; i++)
        {
            PropertyInfo property = propertiesFromJoin[i];
            if (property.Name.ToLower().Contains("id") && i == 0) continue;
            sqlQuery += string.Format("([{0}].{1} LIKE '%" + value + "%')", typeof(T2).Name, property.Name);

            if (i + 1 < propertiesFromJoin.Count)
            {
                sqlQuery += " OR ";
            }
        }
        #endregion

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the _response_ from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a entity holder object to hold the response from the database
        T entity = default(T);

        // Creating a result list to hold the _responses_ from the database
        List<T> result = new List<T>();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Creating the Entity object, it can now be used to set data
                entity = GetGenericType();
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(entity, reader[i], null);
                }
                // Adding the entity to the List and continuing to the next field
                result.Add(entity);
            }
        }

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Gets all elements from table that matches requirements.
    /// </summary>
    /// <typeparam name="T1">First to join</typeparam>
    /// <typeparam name="T2">Second table to join</typeparam>
    /// <typeparam name="T3">Third table to join</typeparam>
    /// <param name="value">The search parameter</param>
    /// <param name="fields">The fields in the table you wish to include</param>
    /// <returns>List of elements from table that matches the value and fields</returns>
    public List<T> SearchByJoin<T1, T2, T3>(object value, params string[] fields)
    {
        // Creating the SELECT SQL Statement, with {0} as Table name
        string sqlQuery = string.Format("SELECT [{0}].* FROM [{0}] ", typeof(T).Name);
        sqlQuery += string.Format("INNER JOIN [{0}] ON [{1}].{0}ID = [{0}].ID ", typeof(T1).Name, typeof(T).Name);
        sqlQuery += string.Format("INNER JOIN [{0}] ON [{1}].{0}ID = [{0}].ID ", typeof(T2).Name, typeof(T).Name);
        sqlQuery += string.Format("INNER JOIN [{0}] ON [{1}].{0}ID = [{0}].ID ", typeof(T3).Name, typeof(T).Name);

        sqlQuery += "WHERE ";

        // Adding fields to the SQL statement
        for (int i = 0; i < fields.Length; i++)
        {
            sqlQuery += string.Format("([{2}].{0} LIKE '%{1}%')", fields[i], value, typeof(T).Name);
            sqlQuery += " OR ";
        }

        #region QEntity
        List<PropertyInfo> propertiesFromJoin = new List<PropertyInfo>(GetGenericType<T1>().GetType().GetProperties());

        for (int i = 0; i < propertiesFromJoin.Count; i++)
        {
            PropertyInfo property = propertiesFromJoin[i];
            if (property.Name.ToLower().Contains("id") && i == 0) continue;
            sqlQuery += string.Format("([{0}].{1} LIKE '%" + value + "%')", typeof(T1).Name, property.Name);

            sqlQuery += " OR ";
        }
        #endregion

        #region PEntity
        propertiesFromJoin = new List<PropertyInfo>(GetGenericType<T2>().GetType().GetProperties());

        for (int i = 0; i < propertiesFromJoin.Count; i++)
        {
            PropertyInfo property = propertiesFromJoin[i];
            if (property.Name.ToLower().Contains("id") && i == 0) continue;
            sqlQuery += string.Format("([{0}].{1} LIKE '%" + value + "%')", typeof(T2).Name, property.Name);

            sqlQuery += " OR ";
        }
        #endregion

        #region OEntity
        propertiesFromJoin = new List<PropertyInfo>(GetGenericType<T3>().GetType().GetProperties());

        for (int i = 0; i < propertiesFromJoin.Count; i++)
        {
            PropertyInfo property = propertiesFromJoin[i];
            if (property.Name.ToLower().Contains("id") && i == 0) continue;
            sqlQuery += string.Format("([{0}].{1} LIKE '%" + value + "%')", typeof(T3).Name, property.Name);

            if (i + 1 < propertiesFromJoin.Count)
            {
                sqlQuery += " OR ";
            }
        }
        #endregion

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the _response_ from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a entity holder object to hold the response from the database
        T entity = default(T);

        // Creating a result list to hold the _responses_ from the database
        List<T> result = new List<T>();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Creating the Entity object, it can now be used to set data
                entity = GetGenericType();
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(entity, reader[i], null);
                }
                // Adding the entity to the List and continuing to the next field
                result.Add(entity);
            }
        }

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Deletes entity by ID
    /// </summary>
    /// <param name="id">ID reference in the database</param>
    public void Delete(int id, out T entity)
    {
        entity = Get(id);
        // Creating the Delete statement, where {0} is the table name, and {1} is the ID parameter from this method
        string sqlQuery = string.Format("DELETE FROM [{0}] WHERE ID = '{1}'", typeof(T).Name, id);

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Executing the Delete statement
        cmd.ExecuteNonQuery();

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();
    }

    /// <summary>
    /// Deletes entity by ID
    /// </summary>
    /// <param name="id">ID reference in the database</param>
    public void Delete(int id)
    {
        // Creating the Delete statement, where {0} is the table name, and {1} is the ID parameter from this method
        string sqlQuery = string.Format("DELETE FROM [{0}] WHERE ID = '{1}'", typeof(T).Name, id);

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Executing the Delete statement
        cmd.ExecuteNonQuery();

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();
    }

    /// <summary>
    /// Deletes ALL references to entity by field and value
    /// </summary>
    /// <param name="field">field reference in the database</param>
    /// <param name="value">value to compare</param>
    public void DeleteBy(string field, object value)
    {
        // Creating the Delete statement, where {0} is the table name, and {1} is the ID parameter from this method
        string sqlQuery = string.Format("DELETE FROM [{0}] WHERE {1} = '{2}'", typeof(T).Name, field, value);

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Executing the Delete statement
        cmd.ExecuteNonQuery();

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();
    }

    /// <summary>
    /// Deletes ALL references to entity by field and value
    /// </summary>
    /// <param name="field1">field reference in the database</param>
    /// <param name="value1">value to compare</param>
    /// <param name="field2">field reference in the database</param>
    /// <param name="value2">value to compare</param>
    public void DeleteBy(string field1, object value1, string field2, object value2)
    {
        // Creating the Delete statement, where {0} is the table name, and {1} is the ID parameter from this method
        string sqlQuery = string.Format("DELETE FROM [{0}] WHERE {1} = '{2}' AND '{3}' = '{4}'", typeof(T).Name, field1, value1, field2, value2);

        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Executing the Delete statement
        cmd.ExecuteNonQuery();

        // disposning and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();
    }

    /// <summary>
    /// Check to see if entity by field and value exists in current table
    /// </summary>
    /// <param name="field">Field to search in</param>
    /// <param name="value">Value to search by</param>
    /// <returns>
    /// false if value in field does not exist.
    /// Usefull if you're checking to see if a entity by name exists already.
    /// </returns>
    public bool ExistsBy(string field, object value)
    {
        // Uses the SearchBy method to find all matching elements
        List<T> matchingElements = SearchBy(value, field);
        // if 1 or more element is found, returns true
        if (matchingElements != null && matchingElements.Count > 0)
        {
            return true;
        }
        // Else return false
        return false;
    }

    public T SqlQuery(string sqlQuery)
    {
        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the response from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a result object to hold the response from the database
        T result = GetGenericType();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(result, reader[i]);
                }
            }
        }

        // Disposing and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Gets latest element by column DESC
    /// </summary>
    /// <param name="column"></param>
    /// <returns>Latest Entity</returns>
    public T GetLatest(string column = "ID", string orderBy = "DESC")
    {
        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        string sqlQuery = string.Format("SELECT TOP 1 * FROM [{0}] ORDER BY [{0}].[{1}] {2}", typeof(T).Name, column, orderBy);

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        // Creating a Reader to contain the response from the database
        SqlDataReader reader = cmd.ExecuteReader();

        // Creating a result object to hold the response from the database
        T result = GetGenericType();

        // Does the server got a respond for us?
        if (reader.HasRows)
        {
            // As long as there is rows to read, do this
            while (reader.Read())
            {
                // Loops through the properties of the current type
                for (int i = 0; i < properties.Count; i++)
                {
                    // If the value from the database is Null, we continue
                    if (reader[i] == DBNull.Value) continue;
                    // Setting the property value as the value from the database
                    properties[i].SetValue(result, reader[i]);
                }
            }
        }

        // Disposing and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Gets a random amount of elements
    /// </summary>
    /// <param name="amount">Amount you need</param>
    /// <returns>A list of entities</returns>
    public List<T> TakeRandom(int amount)
    {
        // First, get all elements
        List<T> allT = GetAll();
        // Instantiate random type, this contains methods to calculate a random number.
        Random rand = new Random();
        // Create the list that will contain the random elements.
        List<T> tmp = new List<T>
        {
            // We start by adding a random element.
            allT[rand.Next(0, allT.Count)]
        };
        // A for loop, to run as long as we need, till we get the amount of elements requested.
        for (int i = 0; i < amount - 1; i++)
        {
            // Get a random entity from the All List
            T type = allT[rand.Next(0, allT.Count)];
            // Then check if that specific element already exists in the list.
            if (tmp.Contains(type))
            {
                // If it does, we take a step back in the for loop, making it run one more time.
                i--;
            }
            else
            {
                // If it doesn't, we add the new element to the temp list.
                tmp.Add(type);
            }
            // Rinse and repeat.
        }

        // In the end, we return the temp list, that now contains 'amount' of random elements.
        return tmp;
    }

    /// <summary>
    /// Counts entities in table
    /// </summary>
    /// <returns></returns>
    public int Count()
    {
        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Creating the query
        string sqlQuery = string.Format("SELECT COUNT(*) FROM {0}", typeof(T).Name);

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        int result = (int)cmd.ExecuteScalar();

        // Disposing and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Counts entities by Colummn and Value
    /// </summary>
    /// <param name="column">Column you wish to filter by</param>
    /// <param name="value">Should be the same as column value.</param>
    /// <returns></returns>
    public int CountBy(string column, object value)
    {
        // We open a connection with the current connectionstring
        SqlConnection connection = new SqlConnection(connectionString);
        connection.Open();

        // Creating the query
        string sqlQuery = string.Format("SELECT COUNT(*) FROM {0} WHERE [{1}] = '{2}'", typeof(T).Name, column, value);

        // Generating the Sql Command to run on the database
        SqlCommand cmd = new SqlCommand(sqlQuery, connection);

        int result = (int)cmd.ExecuteScalar();

        // Disposing and closing connection
        cmd.Dispose();
        connection.Dispose();
        connection.Close();

        // returning result
        return result;
    }

    /// <summary>
    /// Creates a VM between this Entity and select others.
    /// </summary>
    /// <typeparam name="T1">Other Entity</typeparam>
    /// <param name="id">ID for this Entity</param>
    /// <param name="t1ID">ID for QEntity</param>
    /// <returns>A dynamic object that contains a reference to this Entity and QEntity</returns>
    public dynamic GetEntityVM<T1>(int id, int t1ID)
    {
        dynamic vm = new ExpandoObject();
        ((IDictionary<string, object>)vm)[typeof(T).Name] = Get(id);
        ((IDictionary<string, object>)vm)[typeof(T1).Name] = new AutoFactory<T1>().Get(t1ID);
        return vm;
    }

    /// <summary>
    /// Creates a VM between this Entity and select others.
    /// </summary>
    /// <typeparam name="T1">Other Entity</typeparam>
    /// <param name="id">ID for this Entity</param>
    /// <param name="t1ID">ID for QEntity</param>
    /// <param name="t2ID">ID for PEntity</param>
    /// <returns>A dynamic object that contains a reference to this Entity, QEntity and PEntity</returns>
    public dynamic GetEntityVM<T1, T2>(int id, int t1ID, int t2ID)
    {
        dynamic vm = new ExpandoObject();
        ((IDictionary<string, object>)vm)[typeof(T).Name] = Get(id);
        ((IDictionary<string, object>)vm)[typeof(T1).Name] = new AutoFactory<T1>().Get(t1ID);
        ((IDictionary<string, object>)vm)[typeof(T2).Name] = new AutoFactory<T2>().Get(t2ID);
        return vm;
    }

    /// <summary>
    /// Creates a VM between this Entity and select others.
    /// </summary>
    /// <typeparam name="T1">Other Entity</typeparam>
    /// <param name="id">ID for this Entity</param>
    /// <param name="t1ID">ID for QEntity</param>
    /// <param name="t2ID">ID for PEntity</param>
    /// <param name="t3ID">ID for OEntity</param>
    /// <returns>A dynamic object that contains a reference to this Entity, QEntity, PEntity and OEntity</returns>
    public dynamic GetEntityVM<T1, T2, T3>(int id, int t1ID, int t2ID, int t3ID)
    {
        dynamic vm = new ExpandoObject();
        ((IDictionary<string, object>)vm)[typeof(T).Name] = Get(id);
        ((IDictionary<string, object>)vm)[typeof(T1).Name] = new AutoFactory<T1>().Get(t1ID);
        ((IDictionary<string, object>)vm)[typeof(T2).Name] = new AutoFactory<T2>().Get(t2ID);
        ((IDictionary<string, object>)vm)[typeof(T3).Name] = new AutoFactory<T3>().Get(t3ID);
        return vm;
    }

    /// <summary>
    /// Creates a VM between this Entity and select others.
    /// </summary>
    /// <typeparam name="T1">Other Entity</typeparam>
    /// <param name="id">ID for this Entity</param>
    /// <param name="t1ID">ID for QEntity</param>
    /// <param name="t2ID">ID for PEntity</param>
    /// <param name="t3ID">ID for OEntity</param>
    /// <param name="t4ID">ID for UEntity</param>
    /// <returns>A dynamic object that contains a reference to this Entity, QEntity, PEntity, OEntity and UEntity</returns>
    public dynamic GetEntityVM<T1, T2, T3, T4>(int id, int t1ID, int t2ID, int t3ID, int t4ID)
    {
        dynamic vm = new ExpandoObject();
        ((IDictionary<string, object>)vm)[typeof(T).Name] = Get(id);
        ((IDictionary<string, object>)vm)[typeof(T1).Name] = new AutoFactory<T1>().Get(t1ID);
        ((IDictionary<string, object>)vm)[typeof(T2).Name] = new AutoFactory<T2>().Get(t2ID);
        ((IDictionary<string, object>)vm)[typeof(T3).Name] = new AutoFactory<T3>().Get(t3ID);
        ((IDictionary<string, object>)vm)[typeof(T4).Name] = new AutoFactory<T4>().Get(t4ID);
        return vm;
    }
}
