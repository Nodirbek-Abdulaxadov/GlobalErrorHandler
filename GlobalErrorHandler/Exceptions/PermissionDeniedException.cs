﻿namespace GlobalErrorHandler.Exceptions;

public class PermissionDeniedException(string errorMessage = "You have no access")
    : Exception(errorMessage)
{ }