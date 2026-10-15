# Estratégia Automatizada de Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que coloca ordens limite de compra na banda inferior das Bollinger Bands e ordens limite de venda na banda superior. As posições são fechadas quando o preço toca a banda média. As ordens pendentes são atualizadas no início de cada vela.

## Detalhes

- **Critérios de entrada**:
  - Comprado: compra limite na banda inferior das Bollinger Bands
  - Vendido: venda limite na banda superior das Bollinger Bands
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: o preço cruza acima da banda média das Bollinger Bands
  - Vendido: o preço cruza abaixo da banda média das Bollinger Bands
- **Stops**: Nenhum
- **Valores padrão**:
  - `BbPeriod` = 20
  - `BbDeviation` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Não
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
