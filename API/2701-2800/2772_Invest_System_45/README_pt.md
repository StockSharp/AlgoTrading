# Estratégia Invest System 4.5 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Invest System 4.5 é um consultor especialista do MetaTrader 5 que foi portado para a API de estratégia de alto nível do StockSharp. A estratégia opera o par EUR/USD seguindo a direção da vela de 4 horas completa anterior. Uma única operação é permitida durante os primeiros minutos da nova sessão de 4 horas e o dimensionamento de posição se adapta ao desempenho realizado e ao crescimento da conta.

O código se baseia exclusivamente na API de alto nível: assinaturas automáticas de velas são usadas para monitorar tanto o viés direcional de 4 horas quanto a janela de entrada de período inferior, enquanto o helper `StartProtection` integrado aplica níveis estáticos de take-profit e stop-loss expressos em pips.

## Lógica de trading
1. **Viés direcional** – ao fechamento de cada vela de 4 horas concluída, a estratégia armazena se a vela fechou de alta ou de baixa. Uma vela de alta habilita apenas entradas compradas para a próxima sessão, enquanto uma vela de baixa habilita apenas vendidas. Se a vela fechar exatamente na sua abertura, a direção anterior é mantida.
2. **Timing de entrada** – quando uma nova vela de 4 horas começa, uma janela de entrada se abre. A janela permanece válida por um número configurável de minutos (15 por padrão). A estratégia observa velas de período inferior (1 minuto por padrão) e pode enviar no máximo uma ordem de mercado se todos os filtros forem satisfeitos enquanto a janela estiver ativa.
3. **Posição única** – a estratégia nunca faz pirâmide. Se uma posição já estiver aberta, nenhum novo sinal é processado até a próxima sessão de 4 horas. Uma vez enviada uma ordem, a janela de entrada se fecha imediatamente para replicar o comportamento do MetaTrader.
4. **Rastreamento de ganhos e perdas** – quando uma posição é totalmente fechada, o PnL realizado é capturado para impulsionar a lógica adaptativa de lotes descrita abaixo.

## Regras de dimensionamento de posição
O consultor especialista original usa duas camadas de gerenciamento de dinheiro:
- **Marcos de capital**: o saldo inicial da conta é armazenado na primeira atualização. Quando o capital supera 2×, 3× … 6× o saldo inicial, o tamanho de lote base é aumentado proporcionalmente. O Estágio 1 começa em `BaseLot`, o estágio 2 o dobra, o estágio 3 o triplica, e assim por diante. Tamanhos de lote secundários (`Lot2`, `Lot3`, `Lot4`) são derivados usando os multiplicadores originais (×2, ×7 e ×14 respectivamente).
- **Escalada Plano B**: um único valor de volume global é mantido entre as operações.
  - Após uma operação perdedora com o lote base, o volume é elevado para o segundo lote (`Lot3`).
  - Se outra perda ocorrer enquanto se opera com o segundo lote, o "Plano B" se ativa. O Plano B remapeia as opções de lote internas para que o lote base se torne `Lot2` e o lote agressivo se torne `Lot4`. O volume atual não muda imediatamente, mas qualquer perda subsequente empurra a estratégia para o lote agressivo. O Plano B é cancelado automaticamente quando a conta atinge um novo máximo de capital.
  - Uma operação lucrativa sempre redefine o volume atual para o lote base do estágio ativo.
Essas regras reproduzem fielmente a escalada de lotes em cascata da versão MetaTrader sem iterar manualmente por ordens ou usar coleções.

## Gerenciamento de risco
- `StartProtection` configura tanto o stop-loss quanto o take-profit em unidades de preço absoluto derivadas do tamanho do pip. Stops e alvos são registrados apenas uma vez quando a estratégia é iniciada, assim como o EA original anexa os valores a cada ordem.
- Apenas ordens de mercado são usadas. A própria estratégia não realiza posições de hedge, escalamento ou saídas parciais; as saídas ocorrem através das ordens de proteção configuradas.

## Parâmetros da estratégia
| Parâmetro | Descrição | Padrão | Faixa de otimização |
|-----------|-----------|--------|---------------------|
| `StopLossPips` | Distância do stop-loss em pips. Use `0` para desabilitar o stop. | 240 | 120 – 360, passo 20 |
| `TakeProfitPips` | Distância do take-profit em pips. Use `0` para desabilitar o alvo. | 40 | 20 – 80, passo 10 |
| `EntryWindowMinutes` | Duração da janela de entrada após cada nova vela de 4 horas ser aberta. | 15 | 5 – 30, passo 5 |
| `SignalCandleType` | Série de velas usada para monitorar a janela de entrada (1 minuto por padrão). | Período de 1 minuto | – |
| `TrendCandleType` | Vela de período superior usada para construir o viés direcional (4 horas por padrão). | Período de 4 horas | – |
| `BaseLot` | Tamanho de lote inicial para o estágio 1. Outros tamanhos de lote são derivados automaticamente. | 0.1 | 0.05 – 0.3, passo 0.05 |

## Estrutura de arquivos
```
2772_Invest_System_45/
├── CS/
│   └── InvestSystem45Strategy.cs
├── README.md
├── README_ru.md
└── README_zh.md
```

## Notas
- A estratégia espera que o instrumento anexado forneça tanto a série de velas de 4 horas quanto a série de período mais rápido. Essas assinaturas são criadas automaticamente dentro de `OnStarted`.
- O tamanho do pip é determinado a partir de `Security.PriceStep` e ajustado para cotações fracionárias (3 ou 5 casas decimais) para corresponder ao tratamento de valores de pip do MetaTrader.
- Como o robô original usa limites de saldo de conta, a implementação StockSharp lê `Portfolio.CurrentValue` em cada atualização de vela de entrada. Ao executar em simulação, certifique-se de que o modelo de portfólio atualize o capital atual para que o dimensionamento de lotes permaneça consistente.
- A tradução para Python é omitida intencionalmente conforme solicitado.
