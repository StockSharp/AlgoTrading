# Estratégia Exp XWPR Histograma Vol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão em C# do expert do MetaTrader **Exp_XWPR_Histogram_Vol**. Opera nas mudanças de cor do
indicador personalizado XWPR Histograma Vol, que multiplica o oscilador Williams %R pelo volume da vela e suaviza o resultado. O
port mantém o esquema original de gestão de dinheiro de dois slots (volume primário e secundário) e reproduz as mesmas regras de
entrada e saída baseadas em cor usando a API de alto nível do StockSharp.

O algoritmo processa apenas velas finalizadas. Em cada nova barra, inspeciona a cor do histograma um número configurável de barras
atrás no passado e reage quando as transições de cor cruzam os limites de alta ou baixa definidos pelo indicador.

## Lógica do indicador
1. Williams %R (`WprPeriod`) é deslocado em +50 e multiplicado pelo volume de vela selecionado (`VolumeMode`).
2. Tanto o Williams %R ponderado quanto o volume bruto passam por filtros de suavização idênticos (`SmoothingMethod`,
   `SmoothingLength`, `SmoothingPhase`).
3. Quatro níveis dinâmicos são derivados do volume suavizado: `HighLevel2`, `HighLevel1`, `LowLevel1` e `LowLevel2`.
4. As cores do histograma correspondem às zonas definidas por esses níveis:
   - **0** – histograma acima de `HighLevel2` (alta forte).
   - **1** – histograma entre `HighLevel1` e `HighLevel2` (alta moderada).
   - **2** – histograma entre `LowLevel1` e `HighLevel1` (neutro).
   - **3** – histograma entre `LowLevel2` e `LowLevel1` (baixa moderada).
   - **4** – histograma abaixo de `LowLevel2` (baixa forte).

## Regras de sinal
A estratégia lê duas cores históricas por avaliação: barra `SignalBar + 1` (mais antiga) e barra `SignalBar` (mais recente).

- **Abrir comprado primário (volume = `PrimaryVolume`)** quando a cor da barra mais antiga é `1` e a cor da barra mais nova se move para `2`, `3` ou
  `4`. O movimento simultaneamente solicita o fechamento de posições vendidas.
- **Abrir comprado secundário (volume = `SecondaryVolume`)** quando a cor da barra mais antiga é `0` e a cor da barra mais nova se torna
  qualquer coisa diferente de `0`. O mesmo sinal também fecha vendidos.
- **Abrir vendido primário (volume = `PrimaryVolume`)** quando a cor da barra mais antiga é `3` e a cor da barra mais nova sobe para `0`, `1`
  ou `2`, enquanto também fecha comprados.
- **Abrir vendido secundário (volume = `SecondaryVolume`)** quando a cor da barra mais antiga é `4` e a cor da barra mais nova se torna
  `0`, `1`, `2` ou `3`, novamente forçando saídas compradas.
- **Fechar comprados** sempre que a cor mais antiga for `3` ou `4` (zona de baixa).
- **Fechar vendidos** sempre que a cor mais antiga for `0` ou `1` (zona de alta).

Dois slots de posição independentes são mantidos para cada direção. Um sinal só aciona uma ordem se o slot correspondente estiver
atualmente inativo e o indicador de entrada relevante (`AllowLongEntry`, `AllowShortEntry`) permitir.

## Gestão de risco
- `StopLossSteps` e `TakeProfitSteps` são traduzidos para ordens protetoras do StockSharp via `StartProtection`. Os valores são
  expressos em passos de preço do instrumento.
- `DeviationSteps` é preservado para compatibilidade com a lista de entradas MQL. As ordens de mercado do StockSharp não o utilizam.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `CandleType` | Período usado para construir as velas fornecidas ao indicador. |
| `PrimaryVolume`, `SecondaryVolume` | Volumes aplicados pelos slots de nível um e nível dois. |
| `AllowLongEntry`, `AllowShortEntry` | Habilitar abertura de novas posições compradas ou vendidas. |
| `AllowLongExit`, `AllowShortExit` | Habilitar fechamento de exposição comprada ou vendida quando aparecerem sinais de saída. |
| `StopLossSteps`, `TakeProfitSteps` | Distâncias protetoras opcionais em passos de preço (0 desabilita a proteção respectiva). |
| `DeviationSteps` | Reservado para compatibilidade; não tem efeito nas ordens do StockSharp. |
| `SignalBar` | Número de velas fechadas para deslocar a avaliação de sinal (0 = última vela finalizada). |
| `WprPeriod` | Período de retrospectiva para o cálculo do Williams %R. |
| `VolumeMode` | Seleciona entre contagem de ticks (`Tick`) ou volume real (`Real`) no histograma. |
| `HighLevel2`, `HighLevel1` | Multiplicadores que definem os limiares de alta superiores. |
| `LowLevel1`, `LowLevel2` | Multiplicadores que definem os limiares de baixa inferiores. |
| `SmoothingMethod` | Tipo de média móvel usada tanto para o histograma quanto para o volume de referência. |
| `SmoothingLength` | Comprimento dos filtros de suavização. |
| `SmoothingPhase` | Fase encaminhada para suavizadores baseados em Jurik (ignorada por outros métodos). |

## Notas de uso
- A estratégia opera em um único ativo retornado por `GetWorkingSecurities()` e usa ordens de mercado para todas as ações.
- Os sinais são avaliados uma vez por vela finalizada. O buffer de histórico adicional evita ordens duplicadas na mesma barra.
- Os dois slots de entrada atuam de forma independente. Desabilite um slot definindo o volume correspondente como `0` ou desabilitando o
  indicador `Allow*Entry`.
- A conversão não replica os números mágicos do MetaTrader nem os modos de margem. O dimensionamento do portfólio é inteiramente controlado pelos
  parâmetros `PrimaryVolume` e `SecondaryVolume`.
