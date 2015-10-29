﻿using System;
using System.Collections.Generic;
using MVCForum.Domain.DomainModel;

namespace MVCForum.Domain.Interfaces.Repositories
{
    public partial interface IMembershipRepository
    {
        MembershipUser GetUserById(Guid? userId, bool removeTracking = false);
        IList<MembershipUser> SearchMembers(string username, int amount);
        IList<MembershipUser> GetActiveMembers();
        IList<MembershipUser> GetUsersByDaysPostsPoints(int amoutOfDaysSinceRegistered, int amoutOfPosts);
        PagedList<MembershipUser> SearchMembers(string search, int pageIndex, int pageSize);
        MembershipUser GetUserBySlug(string slug);
        MembershipUser GetUserByEmail(string slug);
        IList<MembershipUser> GetUserBySlugLike(string slug);
        IList<MembershipUser> GetUsersById(List<Guid> guids);
        IList<MembershipUser> GetAll();
        PagedList<MembershipUser> GetAll(int pageIndex, int pageSize);
        IList<MembershipUser> GetLatestUsers(int amountToTake);
        IList<MembershipUser> GetLowestPointUsers(int amountToTake);
        int MemberCount();

        MembershipUser Add(MembershipUser item);
        MembershipUser Get(Guid id);
        void Delete(MembershipUser item);
    }
}
