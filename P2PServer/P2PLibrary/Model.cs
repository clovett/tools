namespace P2PLibrary
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using System.Data.SqlClient;
    using System.Collections.Generic;
    using System.Data.Common;

    public partial class Model : DbContext
    {
        SqlConnection connection;

        public Model(SqlConnection existingConnection)
            : base(existingConnection, false)
        {
            connection = existingConnection;
        }

        public virtual DbSet<Client> Clients { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        private bool HasSchemaObject(string sql)
        {
            bool found = false;
            using (SqlCommand cmd = new SqlCommand(sql, this.connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        found = true;
                    }
                }
            }
            return found;
        }

        private bool HasTable(string table)
        {
            return HasSchemaObject(string.Format(@"SELECT *
                                                        FROM   INFORMATION_SCHEMA.TABLES
                                                        WHERE  TABLE_NAME = '{0}' ", table));
        }

        private void CreateTable(string name, string ddl)
        {
            if (!HasTable(name))
            {
                using (SqlCommand cmd = new SqlCommand(ddl, this.connection))
                {
                    int rc = cmd.ExecuteNonQuery();
                }
            }
        }
        private static string ClientsTableDdl =
@"CREATE TABLE [dbo].[Clients] (
    [Id]                UNIQUEIDENTIFIER     NOT NULL,
    [Date]              datetime             NOT NULL,
    [Name]              nvarchar  (256)      NOT NULL,
    [LocalAddress]      nvarchar  (50)       NOT NULL,
    [LocalPort]         nvarchar  (50)       NOT NULL,
    [RemoteAddress]     nvarchar  (50)       NOT NULL,
    [RemotePort]        nvarchar  (50)       NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE NONCLUSTERED INDEX [ClientNameIndex] ON [dbo].[Clients] 
(
	[Name] ASC
)
";
        public void EnsureDatabase()
        {
            if (!HasTable("Clients"))
            {
                CreateTable("Clients", ClientsTableDdl);
            }
        }

        public void AddClient(Client client)
        {
            client.Id = Guid.NewGuid();
            this.Clients.Add(client);
            this.SaveChanges();
        }

        public IEnumerable<Client> FindClientsByName(string name)
        {
            foreach (var client in from i in this.Clients where i.Name == name select i)
            {
                yield return client;
            }
        }

        public Client FindClientById(Guid id)
        {
            foreach (var client in from i in this.Clients where i.Id == id select i)
            {
                return client;
            }
            return null;
        }

        public void RemoveOldEntries()
        {
            DateTime yesterday = DateTime.Today.AddDays(-1).ToUniversalTime();
            foreach (var client in from i in this.Clients where i.Date <= yesterday select i)
            {
                this.Clients.Remove(client);
            }
            this.SaveChanges();
        }

    }
}
