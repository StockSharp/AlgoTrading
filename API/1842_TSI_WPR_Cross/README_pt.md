# Estratégia de Cruzamento TSI WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no cruzamento do True Strength Index (TSI) calculado a partir do oscilador Williams %R.
Quando o TSI cruza acima de sua linha de sinal suavizada, a estratégia entra em posição comprada. Quando o TSI cruza abaixo da linha de sinal, entra em posição vendida.

## Parâmetros
- **Candle Type**: Período de velas usado para cálculo.
- **Williams %R Period**: Número de barras para o indicador Williams %R.
- **Short Length**: Comprimento EMA curto usado no cálculo do TSI.
- **Long Length**: Comprimento EMA longo usado no cálculo do TSI.
- **Signal Length**: Comprimento EMA aplicado ao TSI para formar a linha de sinal.

## Regras de trading
1. Calcular o valor do Williams %R de cada vela completada.
2. Inserir esse valor no indicador True Strength Index.
3. Suavizar o TSI com um EMA para obter a linha de sinal.
4. **Comprar** quando o TSI cruza acima da linha de sinal.
5. **Vender** quando o TSI cruza abaixo da linha de sinal.
6. Posições existentes na direção oposta são fechadas em um novo sinal.

## Notas
- A estratégia usa a API de alto nível com assinaturas automáticas de velas.
- StartProtection é iniciado na inicialização para gerenciamento básico de risco.
- Áreas de gráfico são criadas para visualizar TSI, sua linha de sinal e operações executadas.
