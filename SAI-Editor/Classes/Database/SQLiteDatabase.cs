using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using SAI_Editor.Classes.Database.Classes;
using System.Linq;
using SAI_Editor.Enumerators;

namespace SAI_Editor.Classes.Database
{
    class SQLiteDatabase : Database<SQLiteConnection, SQLiteConnectionStringBuilder, SQLiteParameter, SQLiteCommand, SQLiteTransaction>
    {
        public SQLiteDatabase(string file)
        {
            ConnectionString = new SQLiteConnectionStringBuilder
            {
                DataSource = file,
                Version = 3
            };
        }

        public async Task<List<EventTypeInformation>> GetEventTypeInformation()
        {
            DataTable dt = await ExecuteQuery("SELECT * FROM event_type_information");

            if (dt.Rows.Count == 0)
                return null;

            return (from DataRow row in dt.Rows select BuildEventTypeInformation(row)).ToList();
        }

        public async Task<List<ActionTypeInformation>> GetActionTypeInformation()
        {
            DataTable dt = await ExecuteQuery("SELECT * FROM action_type_information");

            if (dt.Rows.Count == 0)
                return null;

            return (from DataRow row in dt.Rows select BuildActionTypeInformation(row)).ToList();
        }

        public async Task<List<TargetTypeInformation>> GetTargetTypeInformation()
        {
            DataTable dt = await ExecuteQuery("SELECT * FROM target_type_information");

            if (dt.Rows.Count == 0)
                return null;

            return (from DataRow row in dt.Rows select BuildTargetTypeInformation(row)).ToList();
        }

        public async Task<string> GetSpellNameById(int id)
        {
            DataTable dt = await ExecuteQuery("SELECT spellName FROM " + SAI_Editor_Manager.GetSpellTableName() + " WHERE id = '" + id + "'");

            if (dt.Rows.Count == 0)
                return "<Spell not found!>";

            return (string)dt.Rows[0]["spellName"]; //! Always take first index; should not be possible to have multiple instances per id, but still
        }

        private EventTypeInformation BuildEventTypeInformation(DataRow row)
        {
            var eventTypeInformation = new EventTypeInformation
            {
                event_type = row["event_type"] != DBNull.Value ? CustomConverter.ToInt32(row["event_type"]) : -1,
                tooltip = row["tooltip"] != DBNull.Value ? (string)row["tooltip"] : String.Empty,
                parameterString1 = row["parameterString1"] != DBNull.Value ? (string)row["parameterString1"] : "Unused Parameter",
                parameterString2 = row["parameterString2"] != DBNull.Value ? (string)row["parameterString2"] : "Unused Parameter",
                parameterString3 = row["parameterString3"] != DBNull.Value ? (string)row["parameterString3"] : "Unused Parameter",
                parameterString4 = row["parameterString4"] != DBNull.Value ? (string)row["parameterString4"] : "Unused Parameter",
                parameterTooltip1 = row["parameterTooltip1"] != DBNull.Value ? (string)row["parameterTooltip1"] : String.Empty,
                parameterTooltip2 = row["parameterTooltip2"] != DBNull.Value ? (string)row["parameterTooltip2"] : String.Empty,
                parameterTooltip3 = row["parameterTooltip3"] != DBNull.Value ? (string)row["parameterTooltip3"] : String.Empty,
                parameterTooltip4 = row["parameterTooltip4"] != DBNull.Value ? (string)row["parameterTooltip4"] : String.Empty
            };
            return eventTypeInformation;
        }

        private ActionTypeInformation BuildActionTypeInformation(DataRow row)
        {
            var actionTypeInformation = new ActionTypeInformation
            {
                action_type = row["action_type"] != DBNull.Value ? CustomConverter.ToInt32(row["action_type"]) : -1,
                tooltip = row["tooltip"] != DBNull.Value ? (string)row["tooltip"] : String.Empty,
                parameterString1 = row["parameterString1"] != DBNull.Value ? (string)row["parameterString1"] : "Unused Parameter",
                parameterString2 = row["parameterString2"] != DBNull.Value ? (string)row["parameterString2"] : "Unused Parameter",
                parameterString3 = row["parameterString3"] != DBNull.Value ? (string)row["parameterString3"] : "Unused Parameter",
                parameterString4 = row["parameterString4"] != DBNull.Value ? (string)row["parameterString4"] : "Unused Parameter",
                parameterString5 = row["parameterString5"] != DBNull.Value ? (string)row["parameterString5"] : "Unused Parameter",
                parameterString6 = row["parameterString6"] != DBNull.Value ? (string)row["parameterString6"] : "Unused Parameter",
                parameterTooltip1 = row["parameterTooltip1"] != DBNull.Value ? (string)row["parameterTooltip1"] : String.Empty,
                parameterTooltip2 = row["parameterTooltip2"] != DBNull.Value ? (string)row["parameterTooltip2"] : String.Empty,
                parameterTooltip3 = row["parameterTooltip3"] != DBNull.Value ? (string)row["parameterTooltip3"] : String.Empty,
                parameterTooltip4 = row["parameterTooltip4"] != DBNull.Value ? (string)row["parameterTooltip4"] : String.Empty,
                parameterTooltip5 = row["parameterTooltip5"] != DBNull.Value ? (string)row["parameterTooltip5"] : String.Empty,
                parameterTooltip6 = row["parameterTooltip6"] != DBNull.Value ? (string)row["parameterTooltip6"] : String.Empty
            };
            return actionTypeInformation;
        }

        private TargetTypeInformation BuildTargetTypeInformation(DataRow row)
        {
            var targetTypeInformation = new TargetTypeInformation
            {
                target_type = row["target_type"] != DBNull.Value ? CustomConverter.ToInt32(row["target_type"]) : -1,
                tooltip = row["tooltip"] != DBNull.Value ? (string)row["tooltip"] : String.Empty,
                parameterString1 = row["parameterString1"] != DBNull.Value ? (string)row["parameterString1"] : "Unused Parameter",
                parameterString2 = row["parameterString2"] != DBNull.Value ? (string)row["parameterString2"] : "Unused Parameter",
                parameterString3 = row["parameterString3"] != DBNull.Value ? (string)row["parameterString3"] : "Unused Parameter",
                parameterTooltip1 = row["parameterTooltip1"] != DBNull.Value ? (string)row["parameterTooltip1"] : String.Empty,
                parameterTooltip2 = row["parameterTooltip2"] != DBNull.Value ? (string)row["parameterTooltip2"] : String.Empty,
                parameterTooltip3 = row["parameterTooltip3"] != DBNull.Value ? (string)row["parameterTooltip3"] : String.Empty
            };
            return targetTypeInformation;
        }

        public AreaTrigger BuildAreaTrigger(DataRow row)
        {
            var areaTrigger = new AreaTrigger
            {
                id = row["m_id"] != DBNull.Value ? CustomConverter.ToInt32(row["m_id"]) : -1,
                map_id = row["m_mapId"] != DBNull.Value ? CustomConverter.ToInt32(row["m_mapId"]) : 0,
                posX = row["m_posX"] != DBNull.Value ? (double)row["m_posX"] : 0,
                posY = row["m_posY"] != DBNull.Value ? (double)row["m_posY"] : 0,
                posZ = row["m_posZ"] != DBNull.Value ? (double)row["m_posZ"] : 0
            };
            return areaTrigger;
        }
    }
}
