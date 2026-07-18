# Estratégia Arrows & Curves
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão do consultor especialista MQL5 **Exp_Arrows_Curves**.
Ela constrói um canal de preço dinâmico utilizando máximas e mínimas recentes e reage a
rompimentos. A estratégia pode abrir ou fechar posições dependendo das permissões do usuário
e da direção da tendência.

## Lógica da estratégia
- Calcular a máxima mais alta e a mínima mais baixa durante o período configurado.
- Expandir o intervalo em um percentual para formar as linhas externas do canal.
- Criar linhas de stop internas usando um percentual adicional.
- Quando o preço rompe acima do canal superior, entrar comprado; quando cai abaixo
  do canal inferior, entrar vendido.
- As linhas de stop internas acionam saídas de posição quando o lado oposto do
  canal é cruzado.

## Parâmetros
- `SspPeriod` – período de retrocesso para máximas e mínimas.
- `Channel` – percentual de expansão para as linhas principais do canal.
- `StopChannel` – percentual adicional utilizado para as linhas de stop internas.
- `CandleType` – período temporal dos candles.
- `BuyPosOpen` / `SellPosOpen` – permitir abertura de posições compradas/vendidas.
- `BuyPosClose` / `SellPosClose` – permitir fechamento de posições compradas/vendidas.

## Indicadores
- Highest
- Lowest

## Notas
A estratégia opera apenas em candles concluídos. O gerenciamento de stop loss e take profit
não está incluído; as saídas dependem dos cruzamentos do canal.
