# Estratégia Zone Recovery Formula
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **estratégia Zone Recovery Formula** é uma versão do expert advisor do MetaTrader 4 "Zone Recovery Formula". O algoritmo segue uma direção de tendência conduzida por médias móveis e então aplica uma técnica de recuperação por zona para mitigar movimentos adversos de preço. A ideia central é alternar ciclos comprados e vendidos com volume gradualmente crescente até que a ação do preço saia da zona de recuperação definida, travando lucro mesmo após várias reversões.

## Como funciona

1. **Detecção de sinal** - A estratégia assina candles de período (15 minutos por padrão) e acompanha uma média móvel simples rápida e uma lenta. Um cruzamento altista inicia um ciclo de recuperação comprado, enquanto um cruzamento baixista inicia um ciclo vendido.
2. **Ordem inicial** - Quando um novo ciclo começa, a estratégia abre uma posição a mercado com o multiplicador de volume base. As distâncias de take-profit e recuperação são calculadas a partir das configurações de pips e do tamanho do tick do instrumento.
3. **Recuperação por zona** - Se o preço se move contra a posição aberta pela distância de recuperação configurada, a estratégia inverte a direção e aumenta o tamanho da ordem usando a sequência da fórmula original (até o número máximo de operações). Isso cria uma exposição líquida alternada que busca cobrir perdas anteriores quando o preço retorna ao alvo de lucro.
4. **Gestão de lucro** - O algoritmo monitora lucro não realizado:
   - Condições de take-profit monetário e percentual podem fechar todas as posições imediatamente.
   - Gestão trailing opcional captura lucros após um ganho predefinido e os protege com uma distância de trailing stop.
5. **Reinício do ciclo** - Quando alvos de lucro são alcançados ou a proteção trailing fecha a posição, o ciclo de recuperação é reiniciado e a estratégia aguarda o próximo sinal de média móvel.

## Parâmetros principais

- **Usar TP dinheiro / TP dinheiro** - Habilita e configura take-profit monetário.
- **Usar TP % / Percentual TP** - Habilita e configura take-profit percentual baseado no saldo do portfólio.
- **Habilitar trailing / TP trailing / SL trailing** - Ativa captura de lucro trailing e define o nível de ativação junto com a distância de proteção.
- **TP pips / Zona pips** - Distâncias (em pips) que definem o objetivo de take-profit e a zona de gatilho de recuperação.
- **Volume base / Máx. operações** - Tamanho inicial da ordem e número de passos de recuperação permitidos em um ciclo.
- **MA rápida / MA lenta** - Médias móveis que geram sinais de entrada.
- **Offset de lucro** - Ajuste opcional usado na fórmula original de volume de recuperação.

## Observações

- A estratégia usa a API de alto nível do StockSharp com assinaturas de candles e vinculação de indicadores.
- Posições de hedge são emuladas invertendo a direção da posição líquida e escalando volume, mantendo a lógica compatível com a contabilidade de posição líquida do StockSharp.
- Verificações de trailing e take-profit dependem do lucro não realizado calculado a partir do preço atual da posição. Ajuste valores monetários para corresponder ao valor do tick do instrumento.
- Sempre teste em ambiente simulado antes de implantar em uma conta real.

## Arquivos

- `CS/ZoneRecoveryFormulaStrategy.cs` - implementação C# da estratégia.
- `README.md` - este arquivo de documentação em inglês.
- `README_ru.md` - documentação em russo.
- `README_zh.md` - documentação em chinês.
