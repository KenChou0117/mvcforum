﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MVCForum.Data.Context;
using MVCForum.Domain.DomainModel;
using MVCForum.Domain.Interfaces;
using MVCForum.Domain.Interfaces.Repositories;
using MVCForum.Utilities;

namespace MVCForum.Data.Repositories
{
    public class TopicTagRepository : ITopicTagRepository
    {
        private readonly MVCForumContext _context;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context"> </param>
        public TopicTagRepository(IMVCForumContext context)
        {
            _context = context as MVCForumContext;
        }

        public IEnumerable<TopicTag> GetAll()
        {
            return _context.TopicTag.ToList();
        }

        public Dictionary<string, int> GetPopularTags(int? amountToTake)
        {
            amountToTake = amountToTake ?? int.MaxValue;
            var tags = _context.TopicTag
                .Select(s => new { TopicCount = s.Topics.Count(), TopicName = s.Tag })
                .Where(x => x.TopicCount > 0)
                .OrderBy(s => s.TopicCount).Take((int)amountToTake).ToList();

            return tags.ToDictionary(tag => tag.TopicName, tag => tag.TopicCount);
        }

        public TopicTag GetTagName(string tag)
        {
            return _context.TopicTag.SingleOrDefault(x => x.Tag == tag);
        }

        public PagedList<TopicTag> GetPagedGroupedTags(int pageIndex, int pageSize)
        {

            // Get the topics using an efficient
            var results = _context.TopicTag
                                .OrderByDescending(x => x.Tag)
                                .Skip((pageIndex - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();


            // Return a paged list
            return new PagedList<TopicTag>(results, pageIndex, pageSize, results.Count);
        }

        public PagedList<TopicTag> SearchPagedGroupedTags(string search, int pageIndex, int pageSize)
        {

            // Get the topics using an efficient
            var results = _context.TopicTag
                                .Where(x => x.Tag.Contains(search))
                                .OrderBy(x => x.Tag)
                                .Skip((pageIndex - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();


            // Return a paged list
            return new PagedList<TopicTag>(results, pageIndex, pageSize, results.Count);
        }

        public IEnumerable<TopicTag> GetByTopic(Topic topic)
        {
            return _context.TopicTag
                .Where(x => x.Topics == topic)
                .ToList();
        }

        public TopicTag Add(TopicTag topicTag)
        {
            topicTag.Id = GuidComb.GenerateComb();
            _context.TopicTag.Add(topicTag);
            return topicTag;
        }

        public TopicTag Get(Guid id)
        {
            return _context.TopicTag.FirstOrDefault(x => x.Id == id);
        }

        public void Delete(TopicTag item)
        {
            _context.TopicTag.Remove(item);
        }

        public void Update(TopicTag item)
        {
            // Check there's not an object with same identifier already in context
            if (_context.TopicTag.Local.Select(x => x.Id == item.Id).Any())
            {
                throw new ApplicationException("Object already exists in context - you do not need to call Update. Save occurs on Commit");
            }
            _context.Entry(item).State = EntityState.Modified;  
        }
    }
}