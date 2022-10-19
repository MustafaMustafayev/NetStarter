﻿using Project.DAL.GenericRepositories.Abstract;
using Project.Entity.Entities;

namespace Project.DAL.Abstract;

public interface IAuthRepository : IGenericRepository<User>
{
}