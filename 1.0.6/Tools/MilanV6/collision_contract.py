"""Shared exporter collision policy, independent of Blender for unit testing."""
VALID_ROLES = frozenset({'none', 'walkable', 'barrier'})
LEGACY_SOLID_GROUPS = frozenset({'Structure','Gallery','Stairs','Exterior','V3_ReferenceExterior','Shelves','IV4_W02','IV4_W03'})

def collision_role(explicit_role, group, is_animated):
    if explicit_role is not None:
        role = str(explicit_role)
        if role not in VALID_ROLES:
            raise ValueError('Invalid collision_role: ' + role)
        if is_animated and role != 'none':
            raise ValueError('Animated mesh cannot use a static collision proxy')
        return role
    return 'walkable' if not is_animated and group in LEGACY_SOLID_GROUPS else 'none'
