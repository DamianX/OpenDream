using System.Collections.Generic;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    class DreamMetaObjectMatrix : DreamMetaObjectRoot {
        public DreamMetaObjectMatrix(DreamRuntime runtime)
            : base(runtime)
        {}

        public override DreamValue OperatorAdd(DreamValue a, DreamValue b) {
            var clone = Runtime.ObjectTree.CreateObject(DreamPath.Matrix);
            clone.InitSpawn(new DreamProcArguments(new List<DreamValue> {a}));
            clone.SpawnProc("Add", new DreamProcArguments(new List<DreamValue>{b}));
            return new DreamValue(clone);
        }

        public override DreamValue OperatorSubtract(DreamValue a, DreamValue b)
        {
            var clone = Runtime.ObjectTree.CreateObject(DreamPath.Matrix);
            clone.InitSpawn(new DreamProcArguments(new List<DreamValue>{a}));
            clone.SpawnProc("Subtract", new DreamProcArguments(new List<DreamValue>{b}));
            return new DreamValue(clone);
        }

        public override DreamValue OperatorAppend(DreamValue a, DreamValue b) {
            var matrix = a.GetValueAsDreamObjectOfType(DreamPath.Matrix);
            return matrix.SpawnProc("Add", new DreamProcArguments(new List<DreamValue>{b}));
        }

        public override DreamValue OperatorRemove(DreamValue a, DreamValue b) {
            var matrix = a.GetValueAsDreamObjectOfType(DreamPath.Matrix);
            return matrix.SpawnProc("Subtract", new DreamProcArguments(new List<DreamValue>{b}));
        }

        public override DreamValue OperatorMultiply(DreamValue a, DreamValue b)
        {
            var clone = Runtime.ObjectTree.CreateObject(DreamPath.Matrix);
            clone.InitSpawn(new DreamProcArguments(new List<DreamValue> {a}));
            clone.SpawnProc("Multiply", new DreamProcArguments(new List<DreamValue>{b}));
            return new DreamValue(clone);
        }
    }
}
