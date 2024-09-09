---
title: ���j�i���i��r
tags: Blazor ASP.NET MudBlazor PetaPoco MySQL MariaDB
---
# ���j�i���i��r
## �͂��߂�
### �ړI
���p�i�𒲒B����ۂ̎d�����̑I��ɗp���邽�߁A���������W���Ĕ�r�ł���悤�ɂ��܂��B

### ��
#### �r���h
- .NET 8.0
- MudBlazor 7.8.0
- PetaPoco 6.0.677
- MySqlConnector 2.3.7

#### �T�[�o

https://zenn.dev/tetr4lab/articles/ad947ade600764

## �f�[�^�\��
### �_���\��
- �J�e�S��
  - ���O
  - �H�i���ǂ���
  - ����ŗ�
- ���i
  - ���O
  - �J�e�S��: �Q��
  - �P��
  - �\���D��x
- �X��
  - ���O
  - �\���D��x
- ���i
  - ���i(�ō�)
  - ����
  - �P��: ���i(�ō�)/����
  - ����ŗ�
  - ���i: �Q��
  - �X��: �Q��
  - �m�F����
  - �\���D��x

### �e�[�u���X�L�[�}
```sql:MariaDB
DROP TABLE IF EXISTS `categories`;
CREATE TABLE `categories` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `version` int(11) NOT NULL DEFAULT 0,
  `created` datetime NOT NULL DEFAULT current_timestamp(),
  `modified` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `name` varchar(255) NOT NULL,
  `is_food` bit(1) NOT NULL DEFAULT b'0',
  `tax_rate` float NOT NULL,
  `remarks` varchar(255) DEFAULT NULL,
  `priority` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='�J�e�S��';
DELIMITER ;;
CREATE TRIGGER `version_check_before_update_on_categories` BEFORE UPDATE ON `categories` FOR EACH ROW begin
    if new.version <= old.version then
        signal SQLSTATE '45000'
        set MESSAGE_TEXT = 'Version mismatch detected.';
    end if;
END ;;
DELIMITER ;
DROP TABLE IF EXISTS `prices`;
CREATE TABLE `prices` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `version` int(11) NOT NULL DEFAULT 0,
  `created` datetime NOT NULL DEFAULT current_timestamp(),
  `modified` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `price` float DEFAULT NULL COMMENT '���i(�ō�)',
  `quantity` float DEFAULT NULL COMMENT '����',
  `unit_price` float GENERATED ALWAYS AS (if(`price` is null or `quantity` is null,NULL,`price` / `quantity`)) VIRTUAL COMMENT '�P��(�ō�)',
  `tax_rate` float NOT NULL COMMENT '�ŗ�',
  `product_id` bigint(20) NOT NULL,
  `store_id` bigint(20) NOT NULL,
  `confirmed` datetime DEFAULT NULL COMMENT '�m�F����',
  `remarks` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_product_id_products_id` (`product_id`),
  KEY `fk_store_id_stores_id` (`store_id`),
  CONSTRAINT `fk_product_id_products_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`),
  CONSTRAINT `fk_store_id_stores_id` FOREIGN KEY (`store_id`) REFERENCES `stores` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='���i';
DELIMITER ;;
CREATE TRIGGER `version_check_before_update_on_prices` BEFORE UPDATE ON `prices` FOR EACH ROW begin
    if new.version <= old.version then
        signal SQLSTATE '45000'
        set MESSAGE_TEXT = 'Version mismatch detected.';
    end if;
END ;;
DELIMITER ;
DROP TABLE IF EXISTS `products`;
CREATE TABLE `products` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `version` int(11) NOT NULL DEFAULT 0,
  `created` datetime NOT NULL DEFAULT current_timestamp(),
  `modified` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `name` varchar(255) NOT NULL,
  `category_id` bigint(20) NOT NULL,
  `unit` varchar(50) DEFAULT NULL,
  `remarks` varchar(255) DEFAULT NULL,
  `priority` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`),
  KEY `category_id` (`category_id`),
  CONSTRAINT `fk_category_id_categories_id` FOREIGN KEY (`category_id`) REFERENCES `categories` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='���i';
DELIMITER ;;
CREATE TRIGGER `version_check_before_update_on_products` BEFORE UPDATE ON `products` FOR EACH ROW begin
    if new.version <= old.version then
        signal SQLSTATE '45000'
        set MESSAGE_TEXT = 'Version mismatch detected.';
    end if;
END ;;
DELIMITER ;
DROP TABLE IF EXISTS `stores`;
CREATE TABLE `stores` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `version` int(11) NOT NULL DEFAULT 0,
  `created` datetime NOT NULL DEFAULT current_timestamp(),
  `modefied` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `name` varchar(255) NOT NULL,
  `remarks` varchar(255) DEFAULT NULL,
  `priority` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='�X��';
DELIMITER ;;
CREATE TRIGGER `version_check_before_update_on_stores` BEFORE UPDATE ON `stores` FOR EACH ROW begin
    if new.version <= old.version then
        signal SQLSTATE '45000'
        set MESSAGE_TEXT = 'Version mismatch detected.';
    end if;
END ;;
DELIMITER ;
```

## ��ʂƋ@�\
- ����: �i�r�Q�[�V�����o�[
  - ���j���[: �{�^������
    - ���i(�z�[��)�A���i�A�J�e�S���A�X��
  - ������: �t�B�[���h
    - ����: �{�^��
    - �i������: �{�^��
  - �e�[�}: �{�^��
    - ���C�g/�_�[�N���[�h�؂�ւ�
- ���i(�z�[��): �܂肽���݈ꗗ�E�C�����C���ҏW���
  - ��: �g�O��
    - ���i�̐ō�/����؂�ւ���
  - �ꗗ: �܂肽���ݕ\
    - ���i: �O���[�v
      - �J�e�S��: ��
      - ���i: ��
      - ���̐��i�ōi�荞��: �{�^��
      - �������i�̉��i��ǉ�: �{�^��
    - ���i: �s
      - �폜: �{�^��
        - �m�F: �_�C�A���O
      - �X��: ��Z���N�^
      - ���i: ��t�B�[���h
        - �ŗ�: �t�B�[���h
      - ����: ��t�B�[���h
        - �P��: ��
      - ���l: ��t�B�[���h
      - �ҏW: �s�{�^��
        - �m��: �{�^��
        - �j��: �{�^��
- �J�e�S��: �ꗗ�E�C�����C���ҏW���
  - �ꗗ: �\
    - ���i�ꗗ: �s�{�^��
    - �폜: �s�{�^��
      - �m�F: �_�C�A���O
    - �D��x: ��t�B�[���h
    - ���O: ��t�B�[���h
    - �H�i: ��g�O��
    - �ŗ�: ��t�B�[���h
      - �H�i�g�O���ɘA�����Ď����ݒ肳��邪�A���������\
    - ���l: ��t�B�[���h
    - �ҏW: �s�{�^��
      - �m��: �{�^��
      - �j��: �{�^��
  - �t�b�^�[
    - �S�Ă̐��i���ꗗ: �{�^��
    - ���O: �t�B�[���h
    - �ǉ�: �{�^��
- ���i: �ꗗ�E�C�����C���ҏW���
  - �ꗗ: �\
    - ���i�ꗗ: �s�{�^��
    - �폜: �s�{�^��
      - �m�F: �_�C�A���O
    - �D��x: ��t�B�[���h
    - ���O: ��t�B�[���h
    - �J�e�S��: ��Z���N�^
    - ���l: ��t�B�[���h
    - �ҏW: �s�{�^��
      - �m��: �{�^��
      - �j��: �{�^��
  - �t�b�^�[
    - �S�Ẳ��i���ꗗ: �{�^��
    - �J�e�S��: ��Z���N�^
    - ���O: �t�B�[���h
    - �ǉ�: �{�^��
      - �����������i�̉��i���ǉ�
- �X��: �ꗗ�E�C�����C���ҏW���
  - �ꗗ: �\
    - ���i�ꗗ: �s�{�^��
    - �폜: �s�{�^��
      - �m�F: �_�C�A���O
    - �D��x: ��t�B�[���h
    - ���O: ��t�B�[���h
    - ���l: ��t�B�[���h
    - �ҏW: �s�{�^��
      - �m��: �{�^��
      - �j��: �{�^��
  - �t�b�^�[
    - �S�Ẳ��i���ꗗ: �{�^��
    - ���O: �t�B�[���h
    - �ǉ�: �{�^��
