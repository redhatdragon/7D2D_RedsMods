using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

abstract class LogData {
    private List<ulong> _executionTimeNS;  //DON'T TOUCH
    private List<ulong> _timeStamps;  //DON'T TOUCH
    private ulong _totalExecutionTimeNS;  //DON'T TOUCH

    public LogData() {
        _executionTimeNS = new List<ulong>();
        _timeStamps = new List<ulong>();
}

    //public abstract void serialize();
    //public abstract void deserialize();
    public abstract void mergeOtherOfSameType(LogData other);  //Manually cast to same type
    public abstract string getAsString();
    public abstract void setFromString(string str);

    public ulong getTotalExecutionTimeNS() {
        return _totalExecutionTimeNS;
    }

    public void startTime() {
        _totalExecutionTimeNS = TimeUtils.GetNanoseconds();
        _timeStamps.Add(_totalExecutionTimeNS);
    }
    public void endTime() {
        _totalExecutionTimeNS = TimeUtils.GetNanoseconds() - _totalExecutionTimeNS;
    }

    public void mergeBase(LogData other) {  //DON'T TOUCH
        int count = other._executionTimeNS.Count;
        for(int i = 0; i < count; i++) {
            _executionTimeNS.Add(other._executionTimeNS[i]);
            _timeStamps.Add(other._timeStamps[i]);
            _totalExecutionTimeNS += other._totalExecutionTimeNS;
        }
    }
}
class Logger {
    Dictionary<Type, Dictionary<string, LogData>> logs;

    public Logger() {
        logs = new Dictionary<Type, Dictionary<string, LogData>>();
    }

    public void log(string key, LogData data) {
        Type type = data.GetType();
        if (logs.ContainsKey(type)) {
            if (logs[type].ContainsKey(key)) {
                var d = logs[type][key];
                d.mergeBase(data);
                d.mergeOtherOfSameType(data);
                return;
            }
            logs[type][key] = data;
            return;
        }
        Dictionary<string, LogData> newMap = new Dictionary<string, LogData>();
        newMap[key] = data;
        logs.Add(type, newMap);
    }
    public string getAsStr() {
        string retValue = "";

        foreach(var i in logs) {
            foreach(var j in i.Value) {
                retValue += j.Key; retValue += ":";
                retValue += j.Value.getAsString(); retValue += ", ";
                retValue += j.Value.getTotalExecutionTimeNS().ToString();
                retValue += " \n";
            }
        }
        return retValue;
    }
}