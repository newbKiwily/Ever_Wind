#pragma once
#include "Structs.h"

class Enemy
{
public:
	Enemy(int insNum, int enemyId,GameStruct::Vector3 pos);
	~Enemy();
	enum class EnemyState : int
	{
		IDLE = 0,
		MOVE = 1,
		ATTACK = 2,
		DAMAGED = 3
	};

	int getInstancNum();
	EnemyState getCurrentState();
	void setCurrentState(EnemyState state);
	GameStruct::Vector3 getCurrentPosition();
	void setCurrentPosition(GameStruct::Vector3 position);
	int getEnemyId();
	float getHp();
	void setHp(float newHp);
	void takeDamage(float damage);

	int getOwnerDbId();
	void setOwnerDbId(int dbId);
private:
	
	EnemyState currentState;
	GameStruct::Vector3 currentPosition;
	int instanceNum;
	int enemyId;
	float hp;
	int ownerDbId;
};

