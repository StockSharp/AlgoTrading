# Estratégia de Bollinger Bands com DEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina as Bollinger Bands calculadas em velas de 30 minutos com uma Média Móvel Exponencial Dupla (DEMA) de dados diários para operar rompimentos com confirmação de tendência.

Uma configuração comprada ocorre quando uma vela de alta cruza acima da banda inferior enquanto a DEMA sobe, confirmando o impulso ascendente. Uma configuração vendida ocorre quando uma vela de baixa cruza abaixo da banda superior enquanto a DEMA cai. As posições são fechadas quando uma vela de cor oposta cruza a banda exterior contra a operação.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A vela fecha acima da banda inferior e abre abaixo dela E a DEMA diária está aumentando por três dias consecutivos.
  - **Vendido**: A vela fecha abaixo da banda superior e abre acima dela E a DEMA diária está diminuindo por três dias consecutivos.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - **Comprado**: Uma vela de baixa fecha abaixo da banda superior após abrir acima dela.
  - **Vendido**: Uma vela de alta fecha acima da banda inferior após abrir abaixo dela.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `DemaPeriod` = 20
  - `Deviation` = 2
  - `CandleType` = Período de 30 minutos
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands, DEMA
  - Stops: Não
  - Complexidade: Moderado
  - Período: Intradiário com filtro de tendência diária
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
