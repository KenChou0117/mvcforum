﻿using System;
using System.Collections.Generic;
using MVCForum.Domain.DomainModel;

namespace MVCForum.Domain.Interfaces.Repositories
{
    public interface IPostRepository
    {
        IList<Post> GetAll();
        IList<Post> GetLowestVotedPost(int amountToTake);
        IList<Post> GetHighestVotedPost(int amountToTake);
        IList<Post> GetByMember(Guid memberId, int amountToTake);
        IList<Post> GetSolutionsByMember(Guid memberId);
        IList<Post> GetPostsByTopic(Guid topicId);
        IList<Post> GetPostsByMember(Guid memberId);
        IList<Post> GetAllSolutionPosts();

        int PostCount();

        Post Add(Post item);
        Post Get(Guid id);
        void Delete(Post item);
        void Update(Post item);
    }
}