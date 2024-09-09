---
title: 日曜品価格比較
tags: Blazor ASP.NET MudBlazor PetaPoco MySQL MariaDB
---
# 日曜品価格比較
## はじめに
### 目的
日用品を調達する際の仕入れ先の選定に用いるため、売価を収集して比較できるようにします。

### 環境
#### ビルド
- .NET 8.0
- MudBlazor 7.8.0
- PetaPoco 6.0.677
- MySqlConnector 2.3.7

#### サーバ

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
  - 単位
  - 表示優先度
- 店舗
  - 名前
  - 表示優先度
- 価格
  - 価格(税込)
  - 数量
  - 単価: 価格(税込)/数量
  - 消費税率
  - 製品: 参照
  - 店舗: 参照
  - 確認日時
  - 表示優先度

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
  `priority` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='カテゴリ';
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
  `confirmed` datetime DEFAULT NULL COMMENT '確認日時',
  `remarks` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `fk_product_id_products_id` (`product_id`),
  KEY `fk_store_id_stores_id` (`store_id`),
  CONSTRAINT `fk_product_id_products_id` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`),
  CONSTRAINT `fk_store_id_stores_id` FOREIGN KEY (`store_id`) REFERENCES `stores` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='価格';
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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='製品';
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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin COMMENT='店舗';
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
- 共通: ナビゲーションバー
  - メニュー: ボタン並び
    - 価格(ホーム)、製品、カテゴリ、店舗
  - 検索語: フィールド
    - 検索: ボタン
    - 絞込解除: ボタン
  - テーマ: ボタン
    - ライト/ダークモード切り替え
- 価格(ホーム): 折りたたみ一覧・インライン編集画面
  - 税: トグル
    - 価格の税込/抜を切り替える
  - 一覧: 折りたたみ表
    - 製品: グループ
      - カテゴリ: 列
      - 製品: 列
      - この製品で絞り込み: ボタン
      - 同じ製品の価格を追加: ボタン
    - 価格: 行
      - 削除: ボタン
        - 確認: ダイアログ
      - 店舗: 列セレクタ
      - 価格: 列フィールド
        - 税率: フィールド
      - 数量: 列フィールド
        - 単位: 列
      - 備考: 列フィールド
      - 編集: 行ボタン
        - 確定: ボタン
        - 破棄: ボタン
- カテゴリ: 一覧・インライン編集画面
  - 一覧: 表
    - 製品一覧: 行ボタン
    - 削除: 行ボタン
      - 確認: ダイアログ
    - 優先度: 列フィールド
    - 名前: 列フィールド
    - 食品: 列トグル
    - 税率: 列フィールド
      - 食品トグルに連動して自動設定されるが、書き換え可能
    - 備考: 列フィールド
    - 編集: 行ボタン
      - 確定: ボタン
      - 破棄: ボタン
  - フッター
    - 全ての製品を一覧: ボタン
    - 名前: フィールド
    - 追加: ボタン
- 製品: 一覧・インライン編集画面
  - 一覧: 表
    - 価格一覧: 行ボタン
    - 削除: 行ボタン
      - 確認: ダイアログ
    - 優先度: 列フィールド
    - 名前: 列フィールド
    - カテゴリ: 列セレクタ
    - 備考: 列フィールド
    - 編集: 行ボタン
      - 確定: ボタン
      - 破棄: ボタン
  - フッター
    - 全ての価格を一覧: ボタン
    - カテゴリ: 列セレクタ
    - 名前: フィールド
    - 追加: ボタン
      - 生成した製品の価格も追加
- 店舗: 一覧・インライン編集画面
  - 一覧: 表
    - 価格一覧: 行ボタン
    - 削除: 行ボタン
      - 確認: ダイアログ
    - 優先度: 列フィールド
    - 名前: 列フィールド
    - 備考: 列フィールド
    - 編集: 行ボタン
      - 確定: ボタン
      - 破棄: ボタン
  - フッター
    - 全ての価格を一覧: ボタン
    - 名前: フィールド
    - 追加: ボタン
