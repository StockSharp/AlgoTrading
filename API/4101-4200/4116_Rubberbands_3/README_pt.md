# Estratégia dos Elásticos 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão StockSharp do MetaTrader 4 consultor especialista **RUBBERBANDS_3**. Ele mantém dois extremos de preços correntes, abre posições adicionais sempre que o preço se expande por uma distância configurável e liquida toda a sequência quando ocorre um contra-movimento de um determinado tamanho. Após uma retração, a estratégia opcionalmente muda para a direção oposta enquanto monitora uma meta de lucros e perdas no nível da sessão.

> **Observação:** StockSharp opera em posições líquidas. O script MT4 original pode manter ordens longas e curtas simultaneamente, mas a porta fecha a sequência ativa antes de mudar de direção. O comportamento geral de escalar tendências e desfazer-se em retrocessos é preservado.

## Lógica de negociação

1. Registre o preço de fechamento atual como máximo e mínimo em execução (ou reutilize os valores salvos ao reiniciar).
2. Quando o preço subir `PipStep` pontos acima do máximo atual, envie uma ordem de compra a mercado de tamanho `OrderVolume` e atualize o máximo para o novo preço.
3. Quando o preço cair `PipStep` pontos abaixo do mínimo atual, envie uma ordem de venda a mercado de tamanho `OrderVolume` e atualize o mínimo.
4. Se o mercado recuar `BackStep` pontos contra a direção ativa, feche todas as posições nessa direção e estabeleça uma reversão. O lado oposto é aberto quando a sequência anterior estiver totalmente liquidada.
5. Monitore o resultado cumulativo da sessão. Se o lucro realizado mais o lucro aberto atingir `SessionTakeProfit` × `OrderVolume`, feche a sessão. Quando o rebaixamento durante a reversão exceder `SessionStopLoss` × `OrderVolume`, feche tudo também.
6. A alternância `QuiesceNow` evita novas negociações quando a estratégia é plana. O sinalizador `StopNow` pausa toda a lógica e `CloseNow` solicita um nivelamento imediato do portfólio.

Os pedidos são gerados a partir de velas finalizadas do `CandleType` configurado. O período padrão é de um minuto, correspondendo ao tempo do EA original que acionou as verificações no início de cada minuto.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Tamanho base de cada ordem de mercado. | `0.02` |
| `MaxOrders` | Número máximo de posições simultâneas em uma única direção. Entradas adicionais são bloqueadas quando o limite é atingido. | `10` |
| `PipStep` | Distância de expansão em pontos que agrega um novo comércio. | `100` |
| `BackStep` | Contra-movimento em pontos que força uma saída e prepara uma reversão. | `20` |
| `QuiesceNow` | Quando `true`, a estratégia permanece ociosa enquanto nenhuma posição estiver aberta. | `false` |
| `DoNow` | Abre a primeira sequência longa imediatamente após o início da estratégia. | `false` |
| `StopNow` | Sinalizador de parada brusca que impede qualquer processamento adicional. As posições existentes permanecem intocadas. | `false` |
| `CloseNow` | Solicita uma posição plana imediata, acionando fechamentos sequenciais. | `false` |
| `UseSessionTakeProfit` | Habilita o take-profit cumulativo da sessão. | `true` |
| `SessionTakeProfit` | Lucro alvo na moeda da conta por lote usado para fechar a sessão. | `2000` |
| `UseSessionStopLoss` | Ativa o stop-loss cumulativo da sessão. | `true` |
| `SessionStopLoss` | Perda máxima tolerada por lote durante a reversão antes do encerramento da sessão. | `4000` |
| `UseInitialValues` | Ao reiniciar, reutilize os `InitialMax` e `InitialMin` fornecidos manualmente em vez do preço de fechamento mais recente. | `false` |
| `InitialMax` | Extremo superior armazenado reutilizado quando `UseInitialValues` está ativado. | `0` |
| `InitialMin` | Extremo inferior armazenado reutilizado quando `UseInitialValues` está ativado. | `0` |
| `CandleType` | Série de velas processada pela estratégia. O padrão é velas de um minuto. | `TimeFrame(1m)` |

## Gerenciamento de sessão

- **Agregação de lucros:** os lucros realizados são acumulados após cada fechamento completo, enquanto os ganhos não realizados são recalculados a partir da média ponderada dos preços de entrada de todas as posições abertas.
- **Take-profit da sessão:** assim que `SessionTakeProfit` for atingido, a estratégia fecha todas as negociações e redefine os extremos armazenados.
- **Stop-loss da sessão:** durante uma sequência de reversão (`BackStep` acionada), a estratégia rastreia a perda flutuante. Se o rebaixamento exceder `SessionStopLoss`, todas as posições serão liquidadas e a sessão será reiniciada com estatísticas limpas.

## Notas de uso

- A etapa de preço usada para converter pontos em preços é obtida de `Security.PriceStep`. Configure os metadados do instrumento adequadamente; caso contrário, um substituto de `0.0001` será aplicado.
- Como as ordens são compensadas, a estratégia executa o fechamento das negociações antes de abrir na direção oposta. Ao migrar dados legados, esteja ciente de que o histórico de pedidos pode ser diferente das plataformas protegidas.
- A bandeira `DoNow` abre apenas a primeira posição longa. As entradas adicionais seguem as condições regulares de breakout.
- Use `QuiesceNow` quando quiser deixar a estratégia carregada, mas inativa depois de nivelar o livro.
