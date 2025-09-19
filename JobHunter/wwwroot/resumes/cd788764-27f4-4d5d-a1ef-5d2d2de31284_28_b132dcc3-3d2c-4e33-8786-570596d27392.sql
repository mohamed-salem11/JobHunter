use StoreDB;

SELECT product_name, list_price,
  CASE 
    WHEN list_price < 300 THEN 'Economy'
    WHEN list_price BETWEEN 300 AND 999 THEN 'Standard'
    WHEN list_price BETWEEN 1000 AND 2499 THEN 'Premium'
    ELSE 'Luxury'
  END AS price_category
FROM production.products;


SELECT order_id, order_status, order_date,
  CASE order_status
    WHEN 1 THEN 'Order Received'
    WHEN 2 THEN 'In Preparation'
    WHEN 3 THEN 'Order Cancelled'
    WHEN 4 THEN 'Order Delivered'
  END AS status_desc,
  CASE 
    WHEN order_status = 1 AND DATEDIFF(DAY, order_date, GETDATE()) > 5 THEN 'URGENT'
    WHEN order_status = 2 AND DATEDIFF(DAY, order_date, GETDATE()) > 3 THEN 'HIGH'
    ELSE 'NORMAL'
  END AS priority
FROM sales.orders;


SELECT s.staff_id, s.first_name, s.last_name, COUNT(o.order_id) AS order_count,
  CASE 
    WHEN COUNT(o.order_id) = 0 THEN 'New Staff'
    WHEN COUNT(o.order_id) BETWEEN 1 AND 10 THEN 'Junior Staff'
    WHEN COUNT(o.order_id) BETWEEN 11 AND 25 THEN 'Senior Staff'
    ELSE 'Expert Staff'
  END AS staff_level
FROM sales.staffs s
LEFT JOIN sales.orders o ON s.staff_id = o.staff_id
GROUP BY s.staff_id, s.first_name, s.last_name;


SELECT customer_id, first_name, last_name,
  ISNULL(phone, 'Phone Not Available') AS phone,
  email,
  COALESCE(phone, email, 'No Contact Method') AS preferred_contact,
  city, state
FROM sales.customers;


SELECT p.product_name, s.quantity,
  ISNULL(p.list_price / NULLIF(s.quantity, 0), 0) AS price_per_unit,
  CASE 
    WHEN s.quantity = 0 THEN 'Out of Stock'
    WHEN s.quantity IS NULL THEN 'No Stock Info'
    ELSE 'In Stock'
  END AS stock_status
FROM production.products p
LEFT JOIN production.stocks s ON p.product_id = s.product_id AND s.store_id = 1;

-- 6
SELECT customer_id, first_name, last_name,
  COALESCE(street, '') + ', ' + COALESCE(city, '') + ', ' + COALESCE(state, '') + ', ' + COALESCE(zip_code, '00000') AS formatted_address
FROM sales.customers;


WITH customer_spending AS (
  SELECT o.customer_id,
    SUM(oi.quantity * oi.list_price * (1 - oi.discount)) AS total_spent
  FROM sales.orders o
  JOIN sales.order_items oi ON o.order_id = oi.order_id
  GROUP BY o.customer_id
)
SELECT cs.customer_id, c.first_name, c.last_name, cs.total_spent
FROM customer_spending cs
JOIN sales.customers c ON cs.customer_id = c.customer_id
WHERE cs.total_spent > 1500
ORDER BY cs.total_spent DESC;


WITH revenue_per_category AS (
  SELECT p.category_id, SUM(oi.quantity * oi.list_price * (1 - oi.discount)) AS total_revenue
  FROM sales.order_items oi
  JOIN production.products p ON oi.product_id = p.product_id
  GROUP BY p.category_id
),
avg_order_value AS (
  SELECT p.category_id, AVG(oi.quantity * oi.list_price * (1 - oi.discount)) AS avg_order_value
  FROM sales.order_items oi
  JOIN production.products p ON oi.product_id = p.product_id
  GROUP BY p.category_id
)
SELECT c.category_name, r.total_revenue, a.avg_order_value,
  CASE 
    WHEN r.total_revenue > 50000 THEN 'Excellent'
    WHEN r.total_revenue > 20000 THEN 'Good'
    ELSE 'Needs Improvement'
  END AS performance
FROM revenue_per_category r
JOIN avg_order_value a ON r.category_id = a.category_id
JOIN production.categories c ON r.category_id = c.category_id;


WITH monthly_sales AS (
  SELECT YEAR(order_date) AS yr, MONTH(order_date) AS mon, SUM(oi.quantity * oi.list_price * (1 - oi.discount)) AS total_sales
  FROM sales.orders o
  JOIN sales.order_items oi ON o.order_id = oi.order_id
  GROUP BY YEAR(order_date), MONTH(order_date)
),
monthly_growth AS (
  SELECT mon, yr, total_sales,
    LAG(total_sales) OVER (ORDER BY yr, mon) AS prev_month_sales
  FROM monthly_sales
)
SELECT *,
  ROUND(CASE WHEN prev_month_sales IS NULL THEN NULL ELSE ((total_sales - prev_month_sales) / prev_month_sales * 100) END, 2) AS growth_percent
FROM monthly_growth;


SELECT * FROM (
  SELECT category_id, product_name, list_price,
    ROW_NUMBER() OVER (PARTITION BY category_id ORDER BY list_price DESC) AS rn,
    RANK() OVER (PARTITION BY category_id ORDER BY list_price DESC) AS rk,
    DENSE_RANK() OVER (PARTITION BY category_id ORDER BY list_price DESC) AS dr
  FROM production.products
) ranked
WHERE rn <= 3;


WITH spending AS (
  SELECT o.customer_id, SUM(oi.quantity * oi.list_price * (1 - oi.discount)) AS total_spent
  FROM sales.orders o
  JOIN sales.order_items oi ON o.order_id = oi.order_id
  GROUP BY o.customer_id
)
SELECT c.customer_id, c.first_name, c.last_name, s.total_spent,
  RANK() OVER (ORDER BY s.total_spent DESC) AS ranking,
  NTILE(5) OVER (ORDER BY s.total_spent DESC) AS ntile,
  CASE 
    WHEN NTILE(5) OVER (ORDER BY s.total_spent DESC) = 1 THEN 'VIP'
    WHEN NTILE(5) OVER (ORDER BY s.total_spent DESC) = 2 THEN 'Gold'
    WHEN NTILE(5) OVER (ORDER BY s.total_spent DESC) = 3 THEN 'Silver'
    WHEN NTILE(5) OVER (ORDER BY s.total_spent DESC) = 4 THEN 'Bronze'
    ELSE 'Standard'
  END AS tier
FROM spending s
JOIN sales.customers c ON s.customer_id = c.customer_id;


WITH store_perf AS (
  SELECT store_id, SUM(oi.quantity * oi.list_price * (1 - oi.discount)) AS revenue, COUNT(DISTINCT o.order_id) AS orders
  FROM sales.orders o
  JOIN sales.order_items oi ON o.order_id = oi.order_id
  GROUP BY store_id
)
SELECT s.store_id, st.store_name, s.revenue, s.orders,
  PERCENT_RANK() OVER (ORDER BY s.revenue DESC) AS revenue_rank,
  PERCENT_RANK() OVER (ORDER BY s.orders DESC) AS orders_rank
FROM store_perf s
JOIN sales.stores st ON s.store_id = st.store_id;

SELECT category_name, 
       ISNULL([Electra], 0) AS Electra,
       ISNULL([Haro], 0) AS Haro,
       ISNULL([Trek], 0) AS Trek,
       ISNULL([Surly], 0) AS Surly
FROM (
    SELECT c.category_name, b.brand_name
    FROM production.products p
    JOIN production.categories c ON p.category_id = c.category_id
    JOIN production.brands b ON p.brand_id = b.brand_id
) AS src
PIVOT (
    COUNT(brand_name) FOR brand_name IN ([Electra], [Haro], [Trek], [Surly])
) AS pvt;


SELECT store_name, 
       ISNULL([1], 0) AS Jan, ISNULL([2], 0) AS Feb, ISNULL([3], 0) AS Mar,
       ISNULL([4], 0) AS Apr, ISNULL([5], 0) AS May, ISNULL([6], 0) AS Jun,
       ISNULL([7], 0) AS Jul, ISNULL([8], 0) AS Aug, ISNULL([9], 0) AS Sep,
       ISNULL([10], 0) AS Oct, ISNULL([11], 0) AS Nov, ISNULL([12], 0) AS Dec,
       ISNULL([1],0)+ISNULL([2],0)+ISNULL([3],0)+ISNULL([4],0)+ISNULL([5],0)+ISNULL([6],0)+ISNULL([7],0)+ISNULL([8],0)+ISNULL([9],0)+ISNULL([10],0)+ISNULL([11],0)+ISNULL([12],0) AS Total
FROM (
    SELECT s.store_name, MONTH(o.order_date) AS month, oi.quantity * oi.list_price * (1 - oi.discount) AS revenue
    FROM sales.orders o
    JOIN sales.order_items oi ON o.order_id = oi.order_id
    JOIN sales.stores s ON o.store_id = s.store_id
) AS src
PIVOT (
    SUM(revenue) FOR month IN ([1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12])
) AS pvt;


SELECT store_name, 
       ISNULL([1], 0) AS Pending, ISNULL([2], 0) AS Processing, 
       ISNULL([3], 0) AS Rejected, ISNULL([4], 0) AS Completed
FROM (
    SELECT s.store_name, o.order_status
    FROM sales.orders o
    JOIN sales.stores s ON o.store_id = s.store_id
) AS src
PIVOT (
    COUNT(order_status) FOR order_status IN ([1], [2], [3], [4])
) AS pvt;


WITH sales_data AS (
    SELECT b.brand_name, YEAR(o.order_date) AS order_year, 
           oi.quantity * oi.list_price * (1 - oi.discount) AS revenue
    FROM sales.orders o
    JOIN sales.order_items oi ON o.order_id = oi.order_id
    JOIN production.products p ON oi.product_id = p.product_id
    JOIN production.brands b ON p.brand_id = b.brand_id
)
SELECT brand_name,
       ISNULL([2016], 0) AS Sales2016,
       ISNULL([2017], 0) AS Sales2017,
       ISNULL([2018], 0) AS Sales2018,
       CASE 
           WHEN ISNULL([2016],0) = 0 THEN NULL 
           ELSE ROUND((ISNULL([2018],0) - ISNULL([2016],0)) * 100.0 / ISNULL([2016],1), 2)
       END AS PercentGrowth
FROM sales_data
PIVOT (
    SUM(revenue) FOR order_year IN ([2016], [2017], [2018])
) AS pvt;


SELECT p.product_name, 'In Stock' AS status
FROM production.stocks s
JOIN production.products p ON s.product_id = p.product_id
WHERE s.quantity > 0

UNION

SELECT p.product_name, 'Out of Stock'
FROM production.stocks s
JOIN production.products p ON s.product_id = p.product_id
WHERE s.quantity = 0 OR s.quantity IS NULL

UNION

SELECT p.product_name, 'Discontinued'
FROM production.products p
WHERE NOT EXISTS (
    SELECT 1 FROM production.stocks s WHERE s.product_id = p.product_id
);


SELECT customer_id FROM sales.orders WHERE YEAR(order_date) = 2017
INTERSECT
SELECT customer_id FROM sales.orders WHERE YEAR(order_date) = 2018;


SELECT p.product_name, 'In All 3 Stores' AS status
FROM production.products p
JOIN production.stocks s1 ON p.product_id = s1.product_id AND s1.store_id = 1
JOIN production.stocks s2 ON p.product_id = s2.product_id AND s2.store_id = 2
JOIN production.stocks s3 ON p.product_id = s3.product_id AND s3.store_id = 3

UNION


SELECT p.product_name, 'Only in Store 1'
FROM production.products p
JOIN production.stocks s ON p.product_id = s.product_id
WHERE s.store_id = 1
AND p.product_id NOT IN (
    SELECT product_id FROM production.stocks WHERE store_id = 2
);


SELECT customer_id, 'Lost Customer' AS status
FROM (
    SELECT DISTINCT customer_id FROM sales.orders WHERE YEAR(order_date) = 2016
    EXCEPT
    SELECT DISTINCT customer_id FROM sales.orders WHERE YEAR(order_date) = 2017
) AS lost

UNION ALL


SELECT customer_id, 'New Customer' AS status
FROM (
    SELECT DISTINCT customer_id FROM sales.orders WHERE YEAR(order_date) = 2017
    EXCEPT
    SELECT DISTINCT customer_id FROM sales.orders WHERE YEAR(order_date) = 2016
) AS newc

UNION ALL


SELECT customer_id, 'Retained Customer' AS status
FROM (
    SELECT DISTINCT customer_id FROM sales.orders WHERE YEAR(order_date) = 2016
    INTERSECT
    SELECT DISTINCT customer_id FROM sales.orders WHERE YEAR(order_date) = 2017
) AS retained;
