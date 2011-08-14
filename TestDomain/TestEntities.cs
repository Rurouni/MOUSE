﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using MOUSE.Core;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace TestDomain
{
    [Export(typeof(NodeEntity))]
    [NodeEntity(typeof(ITestEntity))]
    public class TestEntity : NodeEntity, ITestEntity
    {
        private int _counter = 0;
        public async Task<int> Simple(int requestId)
        {
            return 42;
        }

        public void SimpleOneWay()
        {
            _counter++;
        }

        public async Task<ComplexData> Complex(int requestId, ComplexData data, string name, List<ComplexData> datas)
        {
            return new ComplexData(requestId, 0, name, new List<string> {"Test1","Test2"}, datas);
        }
    }

    [NodeEntityContract]
    public interface ITestEntity
    {
        [NodeEntityOperation]
        Task<int> Simple(int requestId);

        [NodeEntityOperation(Reliability = MessageReliability.Unreliable)]
        void SimpleOneWay();

        [NodeEntityOperation(Priority = MessagePriority.High, Reliability = MessageReliability.ReliableOrdered)]
        Task<ComplexData> Complex(int requestId, ComplexData data, string name, List<ComplexData> datas);
        
    }

    [DataContract]
    public class ComplexData
    {
        [DataMember]
        public int SomeInt;
        [DataMember]
        public ulong SomeULong;
        [DataMember]
        public string SomeString;
        [DataMember]
        public List<string> SomeArrString;
        [DataMember]
        public List<ComplexData> SomeArrRec;
        
        public ComplexData()
        {}

        public ComplexData(int someInt, ulong someULong, string someString, List<string> someArrString, List<ComplexData> someArrRec)
        {
            SomeInt = someInt;
            SomeULong = someULong;
            SomeString = someString;
            SomeArrString = someArrString;
            SomeArrRec = someArrRec;
        }

        //deep equal for unit testing
        public bool Equals(ComplexData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (ReferenceEquals(null, other.SomeArrString)
                && !ReferenceEquals(null, SomeArrString)) return false;
            if (!ReferenceEquals(null, other.SomeArrString)
                && ReferenceEquals(null, SomeArrString)) return false;
            if (!ReferenceEquals(null, other.SomeArrString)
                && !ReferenceEquals(null, SomeArrString))
            {
                if (SomeArrString.Count != other.SomeArrString.Count)
                    return false;
                for (int i = 0; i < SomeArrString.Count; i++)
                {
                    if (SomeArrString[i] != other.SomeArrString[i])
                        return false;
                }
            }
            if (ReferenceEquals(null, other.SomeArrRec)
                && !ReferenceEquals(null, SomeArrRec)) return false;
            if (!ReferenceEquals(null, other.SomeArrRec)
                && ReferenceEquals(null, SomeArrRec)) return false;
            if (!ReferenceEquals(null, other.SomeArrRec)
                && !ReferenceEquals(null, SomeArrRec))
            {
                if (SomeArrRec.Count != other.SomeArrRec.Count)
                    return false;
                for (int i = 0; i < SomeArrRec.Count; i++)
                {
                    if (!SomeArrRec[i].Equals(other.SomeArrRec[i]))
                        return false;
                }
            }
            return other.SomeInt == SomeInt
                   && other.SomeULong == SomeULong
                   && Equals(other.SomeString, SomeString);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ComplexData)) return false;
            return Equals((ComplexData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = SomeInt;
                result = (result*397) ^ SomeULong.GetHashCode();
                result = (result*397) ^ (SomeString != null ? SomeString.GetHashCode() : 0);
                result = (result*397) ^ (SomeArrString != null ? SomeArrString.GetHashCode() : 0);
                result = (result*397) ^ (SomeArrRec != null ? SomeArrRec.GetHashCode() : 0);
                return result;
            }
        }
    }
}