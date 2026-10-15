# Estratégia VR Setka 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia VR Setka 3** implementa uma abordagem de negociação em grade. A estratégia coloca ordens limite de compra e venda simétricas ao redor do preço atual do mercado. Após uma ordem ser executada, o nível de take-profit é recalculado usando o preço médio de entrada de todas as posições na direção ativa. Novas ordens de grade são colocadas com espaçamento crescente e, opcionalmente, com volume crescente (martingale).

## Parâmetros
- **Start Offset** – distância inicial do preço atual para o primeiro par de ordens limite.
- **Take Profit** – distância do preço médio de entrada onde todas as posições são fechadas com lucro.
- **Grid Distance** – passo base entre os níveis da grade.
- **Step Distance** – distância adicional adicionada para cada nível de grade subsequente.
- **Use Martingale** – quando habilitado, cada nova ordem de grade aumenta seu volume usando o multiplicador.
- **Martingale Multiplier** – fator para o aumento de volume quando o martingale está ativo.
- **Volume** – volume base da ordem para o primeiro nível.
- **Candle Type** – período usado para sincronizar as operações da estratégia.

## Algoritmo
1. No início, a estratégia coloca um **buy limit** abaixo e um **sell limit** acima do preço atual.
2. Quando um lado é executado, a ordem oposta é cancelada.
3. A estratégia recalcula um nível de take-profit comum no preço médio ± *Take Profit*.
4. Se o preço se mover contra a posição, uma nova ordem limite é colocada em **Grid Distance + Step Distance × nível** do preço médio. O volume aumenta se o martingale estiver habilitado.
5. Quando o preço atinge o nível de take-profit, todas as posições nessa direção são fechadas e a grade é redefinida.

## Notas
- A estratégia não abre posições em ambas as direções simultaneamente.
- É necessário um gerenciamento de risco adequado porque o martingale pode aumentar rapidamente o tamanho da posição.
- Funciona com qualquer instrumento suportado pelo StockSharp, desde que o tipo de candle escolhido esteja disponível.
