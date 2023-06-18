using System.Collections.Generic;

namespace BehaviorTree
{
    // Enumeration representing the state of a behavior tree node.
    public enum NodeState
    {
        RUNNING, // The node is currently running.
        SUCCESS, // The node has successfully completed.
        FAILURE  // The node has failed to complete.
    }

    public class Node
    {
        protected NodeState _state;
        public NodeState State { get => _state; }

        private Node _parent;
        protected List<Node> children = new List<Node>();
        private Dictionary<string, object> _dataContext = new Dictionary<string, object>();

        public Node()
        {
            _parent = null;
        }

        public Node(List<Node> children) : this()
        {
            // Set the children nodes for this node.
            SetChildren(children);
        }

        // Method to evaluate the node and determine its state.
        public virtual NodeState Evaluate() => NodeState.FAILURE;

        // Method to set the children nodes for this node.
        public void SetChildren(List<Node> children)
        {
            foreach (Node c in children)
                Attach(c);
        }

        // Method to attach a child node to this node.
        public void Attach(Node child)
        {
            children.Add(child);
            child._parent = this;
        }

        // Method to detach a child node from this node.
        public void Detach(Node child)
        {
            children.Remove(child);
            child._parent = null;
        }

        // Method to retrieve data from the node's data context or its parent nodes' data context.
        public object GetData(string key)
        {
            object val = null;
            if (_dataContext.TryGetValue(key, out val))
                return val;

            Node node = _parent;
            if (node != null)
                val = node.GetData(key);
            return val;
        }

        // Method to clear data from the node's data context or its parent nodes' data context.
        public bool ClearData(string key)
        {
            bool cleared = false;
            if (_dataContext.ContainsKey(key))
            {
                _dataContext.Remove(key);
                return true;
            }

            Node node = _parent;
            if (node != null)
                cleared = node.ClearData(key);
            return cleared;
        }

        // Method to set data in the node's data context.
        public void SetData(string key, object value)
        {
            _dataContext[key] = value;
        }

        public Node Parent { get => _parent; }
        public List<Node> Children { get => children; }
        public bool HasChildren { get => children.Count > 0; }
        public virtual bool IsFlowNode => false;
    }
}
