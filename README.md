---
title: 日曜品価格比較
tags: Blazor ASP.NET MudBlazor PetaPoco MySQL MariaDB
---
# 日曜品価格比較
## はじめに
### 目的
日用品を調達する際の仕入れ先の選定に用いるため、売価を収集して比較できるようにします。

### 環境

https://zenn.dev/tetr4lab/articles/ad947ade600764

## データ構造
### 論理構成
- カテゴリ
  - 名前
  - 食品かどうか
  - 消費税率
- 製品
  - 名前
  - カテゴリ: 参照
- 店舗
  - 名前
- 価格
  - 価格(税込)
  - 数量
  - 単価: 価格(税込)/数量
  - 消費税率
  - 製品: 参照
  - 店舗: 参照
  - 確認日時

### テーブルスキーマ
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
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='カテゴリ';
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
  `price` float DEFAULT NULL COMMENT '価格(税込)',
  `quantity` float DEFAULT NULL COMMENT '数量',
  `unit_price` float GENERATED ALWAYS AS (if(`price` is null or `quantity` is null,NULL,`price` / `quantity`)) VIRTUAL COMMENT '単価(税込)',
  `tax_rate` float NOT NULL COMMENT '税率',
  `product_id` bigint(20) NOT NULL,
  `store_id` bigint(20) NOT NULL,
  `confirmed` datetime NOT NULL DEFAULT current_timestamp() COMMENT '確認日時',
  `remarks` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_product_id_products_id` (`product_id`),
  KEY `fk_store_id_stores_id` (`store_id`),
  CONSTRAINT `fk_product_id_products_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`),
  CONSTRAINT `fk_store_id_stores_id` FOREIGN KEY (`store_id`) REFERENCES `stores` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=71 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='価格';
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
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`),
  KEY `category_id` (`category_id`),
  CONSTRAINT `fk_category_id_categories_id` FOREIGN KEY (`category_id`) REFERENCES `categories` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=32 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='製品';
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
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=16 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='店舗';
DELIMITER ;;
CREATE TRIGGER `version_check_before_update_on_stores` BEFORE UPDATE ON `stores` FOR EACH ROW begin
    if new.version <= old.version then
        signal SQLSTATE '45000'
        set MESSAGE_TEXT = 'Version mismatch detected.';
    end if;
END ;;
DELIMITER ;
```

## 画面と機能
- 価格(ホーム): 一覧画面
  - 絞込解除: ボタン
    - カテゴリまたは製品の絞り込みを解除
  - 最安のみ: トグル
    - 「(現在の対象)全て」と「最安のみ」を切り替え
  - 新規追加: ボタン
  - 一覧: 表
    - 編集: 行ボタン
    - カテゴリ: 列
      - このカテゴリのみに絞り込み: ボタン
    - 製品: 列
      - この製品のみに絞り込み: ボタン
      - 同じ製品を追加して編集: ボタン
    - 店舗: 列
    - 価格: 列
    - 数量: 列
    - 単位: 列
    - 単価: 列
    - 確認日時: 列
    - 備考: 列
    - 編集: 行ボタン
      - 価格編集へ
- 価格: 編集ダイアログ
  - 製品: テキスト
  - キャンセル: ボタン
  - 保存: ボタン
  - 削除: ボタン
    - 確認: ダイアログ
      - 特に、製品価格の最後のひとつを削除しようとしたとき
  - 価格: フィールド
  - 数量: フィールド
  - ストア: セレクタ
  - 税: トグル
    - [税込み/税抜き]の状態に応じて、税率フィールドの扱いが切り替わる
  - 税率: フィールド
  - 確認日時: ピッカー
    - 価格が無効な値から有効な値に更新された日時が自動的に記録されるが、書き換え可能
  - 備考: フィールド
- カテゴリ: 一覧・インライン編集画面
  - 新規追加: ボタン
    - 名前: フィールド
  - 一覧: 表
    - 名前: 列フィールド
    - 食品: 列トグル
    - 税率: 列フィールド
      - 食品トグルに連動して自動設定されるが、書き換え可能
    - 備考: 列フィールド
    - 削除: 行ボタン
    - 一覧: 行ボタン
      - 選択したカテゴリの製品を一覧
- 製品: 一覧・インライン編集画面
  - 絞込解除: ボタン
    - カテゴリの絞り込みを解除
  - 新規追加: ボタン
    - 名前: フィールド
  - 一覧: 表
    - 名前:列フィールド
    - カテゴリ: 列セレクタ
    - 備考: 列フィールド
    - 削除: 行ボタン
    - 価格追加: 行ボタン
      - 選択した製品の価格を追加
    - 価格一覧: 行ボタン
      - 選択した製品の価格を一覧
- 店舗: 一覧・インライン編集画面
  - 新規追加: ボタン
    - 名前: フィールド
  - 一覧: 表
    - 名前:列フィールド
    - 備考: 列フィールド
    - 削除: 行ボタン


