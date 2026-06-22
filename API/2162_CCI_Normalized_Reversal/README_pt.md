# Estratégia de Reversão Normalizada CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o Índice de Canal de Commodities (CCI) para detectar reversões após o indicador sair de zonas extremas.

## Visão geral

O indicador é calculado em velas de 8 horas com um período configurável. Dois níveis de limiar definem áreas de sobrecompra e sobrevenda. Quando o CCI volta a entrar dentro desses limites após atingir um extremo, a estratégia entra em posição na direção oposta, esperando uma reversão à média.

## Regras de operação

- **Entrada Comprado**: Duas barras atrás o CCI estava acima do nível alto e a barra anterior caiu abaixo dele.
- **Entrada Vendido**: Duas barras atrás o CCI estava abaixo do nível baixo e a barra anterior subiu acima dele.
- **Fechar Comprado**: O CCI da barra anterior estava abaixo do nível médio.
- **Fechar Vendido**: O CCI da barra anterior estava acima do nível médio.

## Parâmetros

- `CciPeriod` – período de retroação para o CCI.
- `HighLevel` – limiar superior do CCI considerado sobrecompra.
- `MiddleLevel` – limiar médio utilizado para sair de posições.
- `LowLevel` – limiar inferior do CCI considerado sobrevenda.
- `CandleType` – série de velas utilizada para os cálculos (padrão 8 horas).

## Notas

A estratégia abre no máximo uma posição por vez e utiliza ordens a mercado. O gerenciamento de risco padrão é habilitado via `StartProtection`.
