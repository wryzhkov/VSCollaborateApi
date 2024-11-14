using Npgsql;
using System.Xml.Linq;
using VsCollaborateApi.Models;

namespace VsCollaborateApi.Services
{
    public class DatabaseClient : IDatabaseClient
    {
        private readonly string _connectionString;

        public DatabaseClient(string connectionString)
        {
            _connectionString = connectionString;
            CreateTables();
        }

        private void CreateTables()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            var sql = """
                create table if not exists document(
                	id uuid primary key,
                	name varchar(100),
                	owner varchar(100)
                );

                create table if not exists users(
                	id uuid primary key,
                	name varchar(100),
                	email varchar(100) unique,
                	password varchar(100)
                );

                """;

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.ExecuteNonQuery();
        }

        // CREATE
        public async Task<bool> CreateDocument(Document document)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "INSERT INTO document (id, name, owner) VALUES (@id, @name, @owner)";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", document.Id);
            cmd.Parameters.AddWithValue("name", document.Name);
            cmd.Parameters.AddWithValue("owner", document.Owner);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // READ
        public async Task<List<Document>> ListDocumentsAsync()
        {
            var documents = new List<Document>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT id, name, owner FROM document";
            using var cmd = new NpgsqlCommand(sql, connection);
            using var reader = await cmd.ExecuteReaderAsync();

            while (reader.Read())
            {
                documents.Add(new Document(reader.GetGuid(0), reader.GetString(1), reader.GetString(2)));
            }

            return documents;
        }

        // UPDATE
        public async Task UpdateDocumentAsync(Document document)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "UPDATE document SET name = @name, age = @age WHERE id = @id";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", document.Id);
            cmd.Parameters.AddWithValue("name", document.Name);
            cmd.Parameters.AddWithValue("owner", document.Owner);

            await cmd.ExecuteNonQueryAsync();
        }

        // DELETE
        public async Task DeleteDocumentAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "DELETE FROM document WHERE id = @id";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id);
        }

        public async Task<Document?> FindDocumentAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT id, name, owner FROM document where id = @id";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", id);

            using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                return new Document(reader.GetGuid(0), reader.GetString(1), reader.GetString(2));
            }

            return null;
        }

        public async Task<User?> FindUserAsync(string email)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT email, name FROM users where email = @email LIMIT 1";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("email", email);

            using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                return new User(reader.GetString(0), reader.GetString(1));
            }

            return null;
        }

        public async Task<bool> CreateUser(User user, string passwordHash)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "INSERT INTO users (id, email, name, password) VALUES (@id, @email, @name, @password)";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("id", Guid.NewGuid());
            cmd.Parameters.AddWithValue("email", user.Email);
            cmd.Parameters.AddWithValue("name", user.Name);
            cmd.Parameters.AddWithValue("password", passwordHash);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> CheckPassword(string email, string passwordHash)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT email FROM users where email = @email and password = @password LIMIT 1";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("email", email);
            cmd.Parameters.AddWithValue("password", passwordHash);

            using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                return true;
            }
            return false;
        }

        public async Task<string> GetPassword(User user)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var sql = "SELECT password FROM users where email = @email LIMIT 1";
            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("email", user.Email);

            using var reader = await cmd.ExecuteReaderAsync();

            if (reader.Read())
            {
                return reader.GetString(0);
            }

            return null;
        }
    }
}