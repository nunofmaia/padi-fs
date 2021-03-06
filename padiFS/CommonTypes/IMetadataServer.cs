﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

namespace padiFS
{
    public interface IMetadataServer
    {
        // Project API
        Metadata Open(string clientName, string filename);
        void Close(string clientName, string filename);
        Metadata Create(string clientName, string filename, int serversNumber, int readQuorum, int writeQuorum);
        void Delete(string clientName, string filename);
        void Fail();
        void Recover();
        string Dump();

        // Auxiliar API
        void RegisterMetadataServer(string name, string address);
        void RegisterDataServer(string name, string address);
        void RegisterClient(string name, string address);
        long GetToken();
        MetadataInfo GetMetadataInfo();
        void UpdateFileMetada(string name, string address);
        bool Ping();
        void SetPrimary(string name);
        string GetPrimary();
        void Recovered(string name);

        void AppendToLog(string command);
        void UpdateLog(string[] log);
        string[] GetLog(int logIndex);

        void DeserializeServer();
    }
}
