public enum CollisionLayerEnum : uint {
  NONE = 0,

  WORLD = 1,
  PLAYER = 2,
  ENEMY= 4,
  PICKUP = 8,

  PLAYER_HITBOX = 16,
  PLAYER_HURTBOX = 32,
  ENEMY_HITBOX = 64,
  ENEMY_HURTBOX = 128,
}

