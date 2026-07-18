# Estratégia DCA Simulation para CryptoCommunity
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia simula o custo médio em dólares com ordens de segurança opcionais e um take-profit com trailing. Começa com uma ordem base e pode investir capital adicional periodicamente ou reduzir o preço médio após quedas de preço.

## Detalhes

- **Critérios de entrada**:
  - Quando não há posição aberta e a data está dentro do intervalo configurado, comprar uma quantidade base.
  - Ordens DCA periódicas opcionais a cada N candles.
  - Ordens de segurança opcionais quando o preço cai um percentual especificado a partir da máxima recente.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Take profit em um percentual alvo, opcionalmente com trailing stop.
- **Stops**: Take profit / trailing stop.
- **Valores padrão**:
  - Ordem base = 100 USD.
  - Valor DCA = 10 USD a cada 30 candles.
  - Valor da ordem de segurança = 100 USD com 15% de desvio de preço.
  - Take profit = 1000%, trailing = 25%.
  - Data de início = 2021-11-01, data de fim = 9999-01-01.
- **Filtros**:
  - Categoria: Acumulação.
  - Direção: Comprado.
  - Indicadores: Nenhum.
  - Stops: Sim.
  - Complexidade: Moderado.
  - Período: Qualquer.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.
