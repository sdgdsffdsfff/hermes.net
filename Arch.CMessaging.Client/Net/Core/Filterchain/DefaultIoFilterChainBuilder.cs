﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Arch.CMessaging.Client.Net.Core.Filterchain
{
    public class DefaultIoFilterChainBuilder : IoFilterChainBuilder
    {
        private readonly List<EntryImpl> _entries;
        private readonly Object _syncRoot;

        public DefaultIoFilterChainBuilder()
        {
            _entries = new List<EntryImpl>();
            _syncRoot = ((ICollection)_entries).SyncRoot;
        }

        public DefaultIoFilterChainBuilder(DefaultIoFilterChainBuilder filterChain)
        {
            if (filterChain == null)
                throw new ArgumentNullException("filterChain");
            _entries = new List<EntryImpl>(filterChain._entries);
            _syncRoot = ((ICollection)_entries).SyncRoot;
        }
        public IEntry<IoFilter, INextFilter> GetEntry(String name)
        {
            return _entries.Find(e => e.Name.Equals(name));
        }

   
        public IoFilter Get(String name)
        {
            IEntry<IoFilter, INextFilter> entry = GetEntry(name);
            return entry == null ? null : entry.Filter;
        }

        public IEnumerable<IEntry<IoFilter, INextFilter>> GetAll()
        {
            foreach (EntryImpl item in _entries)
            {
                yield return item;
            }
        }

        public Boolean Contains(String name)
        {
            return GetEntry(name) != null;
        }

        public void AddFirst(String name, IoFilter filter)
        {
            lock (_syncRoot)
            {
                Register(0, new EntryImpl(this, name, filter));
            }
        }

  
        public void AddLast(String name, IoFilter filter)
        {
            lock (_syncRoot)
            {
                Register(_entries.Count, new EntryImpl(this, name, filter));
            }
        }

     
        public void AddBefore(String baseName, String name, IoFilter filter)
        {
            lock (_syncRoot)
            {
                CheckBaseName(baseName);

                Int32 i = _entries.FindIndex(e => e.Name.Equals(baseName));
                if (i >= 0)
                    Register(i, new EntryImpl(this, name, filter));
            }
        }

        public void AddAfter(String baseName, String name, IoFilter filter)
        {
            lock (_syncRoot)
            {
                CheckBaseName(baseName);
                Int32 i = _entries.FindIndex(e => e.Name.Equals(baseName));
                if (i >= 0)
                    Register(i + 1, new EntryImpl(this, name, filter));
            }
        }

        public IoFilter Remove(String name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            lock (_syncRoot)
            {
                EntryImpl entry = _entries.Find(e => e.Name.Equals(name));
                if (entry != null)
                {
                    _entries.Remove(entry);
                    return entry.Filter;
                }
            }

            throw new ArgumentException("Unknown filter name: " + name);
        }

       
        public IoFilter Replace(String name, IoFilter newFilter)
        {
            lock (_syncRoot)
            {
                CheckBaseName(name);
                EntryImpl e = (EntryImpl)GetEntry(name);
                IoFilter oldFilter = e.Filter;
                e.Filter = newFilter;
                return oldFilter;
            }
        }

        
        /// Removes all filters added to this chain.
        
        public void Clear()
        {
            lock (_syncRoot)
            {
                _entries.Clear();
            }
        }

        
        public void BuildFilterChain(IoFilterChain chain)
        {
            foreach (EntryImpl entry in _entries)
            {
                chain.AddLast(entry.Name, entry.Filter);
            }
        }

        private void CheckBaseName(String baseName)
        {
            if (baseName == null)
                throw new ArgumentNullException("baseName");
            if (!Contains(baseName))
                throw new ArgumentException("Unknown filter name: " + baseName);
        }

        private void Register(Int32 index, EntryImpl e)
        {
            if (Contains(e.Name))
                throw new ArgumentException("Other filter is using the same name: " + e.Name);
            _entries.Insert(index, e);
        }

        class EntryImpl : IEntry<IoFilter, INextFilter>
        {
            private readonly DefaultIoFilterChainBuilder _chain;
            private readonly String _name;
            private IoFilter _filter;

            public EntryImpl(DefaultIoFilterChainBuilder chain, String name, IoFilter filter)
            {
                if (name == null)
                    throw new ArgumentNullException("name");
                if (filter == null)
                    throw new ArgumentNullException("filter");

                _chain = chain;
                _name = name;
                _filter = filter;
            }

            public String Name
            {
                get { return _name; }
            }

            public IoFilter Filter
            {
                get { return _filter; }
                set { _filter = value; }
            }

            public INextFilter NextFilter
            {
                get { throw new InvalidOperationException(); }
            }

            public override String ToString()
            {
                return "(" + _name + ':' + _filter + ')';
            }

            public void AddAfter(String name, IoFilter filter)
            {
                _chain.AddAfter(Name, name, filter);
            }

            public void AddBefore(String name, IoFilter filter)
            {
                _chain.AddBefore(Name, name, filter);
            }

            public void Remove()
            {
                _chain.Remove(Name);
            }

            public void Replace(IoFilter newFilter)
            {
                _chain.Replace(Name, newFilter);
            }
        }
    }
}
