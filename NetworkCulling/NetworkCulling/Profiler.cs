using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.CompilerServices;
using System.Diagnostics;

/*
WARNING: Still unfinished!
*/

public struct Snapshot {
    public string location;
    public float time;
}

public class Profiler {
    const bool isActive = true;
    const int max_snapshots = 100000;
    static Snapshot[] snapshots = new Snapshot[max_snapshots];
    static int snapshotCount = 0;

    static public void timeStart(string message = "",
    [CallerFilePath] string filePath = "",
    [CallerLineNumber] int lineNumber = 0) {
        if (isActive == false)
            return;
        Snapshot snapshot;
        snapshot.location = "File: " +filePath + ", Line:" + lineNumber + ", " + message;
        snapshot.time = 0;
    }
    static public void timeEnd() {

    }
}
