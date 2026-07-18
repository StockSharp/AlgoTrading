# Estratégia de Reversão Contratendência de Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Procura várias barras consecutivas em alta ou em baixa e realiza operações contratendência quando o preço atinge os extremos do canal.

## Detalhes

- **Critérios de entrada**: série de altas ou baixas com confirmação opcional de volume e canal
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `NoOfRises` = 3
  - `NoOfFalls` = 3
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Keltner Channel ou Bollinger Bands
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
