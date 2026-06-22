# Estratégia Oscilador de Peso Direto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o especialista do MetaTrader **Exp_WeightOscillator_Direct** dentro da API de alto nível do StockSharp. Ela combina quatro osciladores clássicos—RSI, Money Flow Index, Williams %R e DeMarker—em um único composto ponderado. O sinal composto é suavizado por uma média móvel configurável e usado para detectar oscilações de momentum. Um composto em alta abre operações compradas (ou fecha vendidas) quando a estratégia trabalha no modo "Direct", enquanto o modo "Against" inverte a lógica para trading contrário.

## Pipeline de indicadores
1. **Relative Strength Index (RSI)** – escala normalizada 0..100.
2. **Money Flow Index (MFI)** – oscilador sensível à liquidez na faixa 0..100.
3. **Williams %R (WPR)** – deslocado por +100 para alinhar com a escala 0..100.
4. **DeMarker** – multiplicado por 100 para corresponder aos outros osciladores.
5. **Média de suavização** – uma das médias móveis suportadas (Simples, Exponencial, Suavizado, Ponderado, Jurik, Kaufman).
6. **Oscilador composto** – média ponderada das entradas normalizadas, suavizada para remover ruído.

O valor do oscilador ponderado é armazenado para cada vela terminada. Os sinais analisam os últimos três valores armazenados, opcionalmente pulando um número de barras mais recentes via parâmetro *Signal Bar* para imitar o comportamento do especialista original.

## Lógica de trading
1. Aguardar até que todos os indicadores e a média móvel de suavização estejam completamente formados.
2. Calcular o oscilador composto suavizado para a barra terminada atual e adicioná-lo ao histórico.
3. Recuperar três valores históricos: `current`, `previous`, `prior`, com índices controlados pelo *Signal Bar*.
4. Detectar mudanças de inclinação:
   - **Ascendente** quando `previous < prior` **e** `current > previous`.
   - **Descendente** quando `previous > prior` **e** `current < previous`.
5. Dependendo do *Trend Mode* selecionado:
   - **Direct**: operar com a inclinação (`ascendente` → sinal comprado, `descendente` → sinal vendido).
   - **Against**: operar contra a inclinação (`ascendente` → vendido, `descendente` → comprado).
6. Aplicar os interruptores de entrada/saída:
   - Fechar a exposição oposta se o interruptor *Close* correspondente estiver habilitado.
   - Abrir novas posições apenas se o interruptor *Allow* respectivo estiver habilitado. O tamanho da ordem equivale a `Volume + |Position|` para que a estratégia possa girar de vendido para comprado (ou vice-versa) com uma única ordem de mercado.
7. As proteções opcionais de stop-loss e take-profit são ativadas por meio de `StartProtection` usando distâncias expressas em passos de preço.

## Parâmetros
| Grupo | Nome | Descrição |
|-------|------|-------------|
| General | **Candle Type** | Período para assinatura de dados e cálculos de indicadores. |
| Trading | **Trend Mode** | `Direct` segue a inclinação do oscilador, `Against` opera contratendência. |
| Trading | **Signal Bar** | Número de barras fechadas mais recentes a pular (1 = última barra fechada). |
| Oscillator | **RSI / MFI / WPR / DeMarker Weight** | Contribuição relativa de cada oscilador na mistura ponderada. Zero desativa um componente. |
| Oscillator | **RSI / MFI / WPR / DeMarker Period** | Comprimento de lookback para cada oscilador. |
| Oscillator | **Smoothing Method** | Média móvel aplicada ao composto (Simples, Exponencial, Suavizado, Ponderado, Jurik, Kaufman). |
| Oscillator | **Smoothing Length** | Período para a média de suavização. |
| Risk Management | **Stop Loss Points** | Distância em passos de preço; `0` desativa o stop. |
| Risk Management | **Take Profit Points** | Distância em passos de preço; `0` desativa o alvo. |
| Trading | **Allow Long/Short Entries** | Habilitar ou desabilitar a abertura de novas posições compradas/vendidas. |
| Trading | **Close Shorts/Longs on Signal** | Permitir fechar exposição existente quando um sinal oposto chega. |

Todos os parâmetros numéricos são expostos como objetos `StrategyParam`, permitindo otimização dentro do Designer do StockSharp.

## Notas de uso
- Configure a propriedade `Volume` base antes de iniciar a estratégia. As ordens de mercado serão escaladas automaticamente ao reverter posições.
- A estratégia assina exatamente uma série de velas retornada por `GetWorkingSecurities()`.
- Os stops protetores usam o `PriceStep` do instrumento para converter distâncias em pontos em valores de preço absolutos.
- Quando *Trend Mode* é definido como `Against`, apenas a polaridade do sinal muda; todas as outras mecânicas permanecem idênticas ao consultor especialista original.
- Williams %R e DeMarker são normalizados para compartilhar a mesma escala 0..100 que RSI/MFI, correspondendo à lógica do indicador original.

## Diferenças do especialista MQL
- O indicador original suportava tipos de suavização adicionais (`ParMA`, `JurX`, `VIDYA`, `T3`). No StockSharp, a estratégia oferece contrapartes de alta qualidade (Jurik e Kaufman) usando Jurik por padrão para compatibilidade.
- Money Flow Index sempre usa o volume agregado da vela. O MetaTrader podia alternar entre volumes de tick e reais; essa escolha depende da fonte de dados no StockSharp.
- O gerenciamento de risco é implementado por meio de `StartProtection` (baseado em passos de preço) em vez de solicitações baseadas em pontos, mas oferece o mesmo comportamento quando `PriceStep` corresponde ao tamanho do contrato do instrumento.

## Primeiros passos
1. Vincule a estratégia a uma carteira e ativo que suporte o tipo de vela configurado.
2. Ajuste os pesos/períodos do indicador e habilite ou desabilite os interruptores de entrada.
3. Escolha o método e comprimento de suavização que melhor se adequem à volatilidade do instrumento.
4. Configure as distâncias de stop-loss/take-profit em passos de preço se a proteção for necessária.
5. Execute a estratégia; os sinais só serão executados em velas terminadas, garantindo comportamento determinístico.
