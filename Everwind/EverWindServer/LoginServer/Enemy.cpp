#include "Enemy.h"

Enemy::Enemy(int insNum, int enemyId, GameStruct::Vector3 pos)
	: instanceNum(insNum), enemyId(enemyId), currentPosition(pos), hp(100.0f), ownerDbId(-1)
{
	currentState = EnemyState::IDLE;
}

Enemy::~Enemy()
{
}

int Enemy::getInstancNum()
{
	return instanceNum;
}

Enemy::EnemyState Enemy::getCurrentState()
{
	return currentState;
}

void Enemy::setCurrentState(EnemyState state)
{
	currentState = state;
}

GameStruct::Vector3 Enemy::getCurrentPosition()
{
	return currentPosition;
}

void Enemy::setCurrentPosition(GameStruct::Vector3 position)
{
	currentPosition = position;
}

int Enemy::getEnemyId()
{
	return enemyId;
}

float Enemy::getHp()
{ 
	return hp;
}
void Enemy::setHp(float newHp)
{
	hp = newHp; 
}

void Enemy::takeDamage(float damage)
{
	hp -= damage;
	if (hp < 0.0f) hp = 0.0f;
}

int Enemy::getOwnerDbId() {
	return ownerDbId;
}
void Enemy::setOwnerDbId(int dbId)
{
	ownerDbId = dbId;
}