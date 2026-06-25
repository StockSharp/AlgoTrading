# Estratégia Exp CandlesticksBW Tm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o especialista MetaTrader **Exp_CandlesticksBW_Tm** sobre a API de alto nível do StockSharp. Ela depende do indicador Candlesticks BW de Bill Williams, que pinta as cores das velas combinando o Awesome Oscillator (AO) e o Accelerator Oscillator (AC). A aceleração e desaceleração do momentum são detectadas por meio de transições de cores das velas, enquanto um filtro opcional de sessão de trading restringe as entradas a horas intradiárias específicas.

## Como funciona

1. Subscreve ao período configurado (padrão **H4**) e alimenta cada vela finalizada em um Awesome Oscillator (5/34). A série AO é suavizada com uma média móvel simples de 5 períodos para produzir o componente Accelerator.
2. Cada vela é classificada em um de seis estados de cor: duas cores de momentum altista (AO e AC subindo), duas cores de momentum baixista (AO e AC caindo) e duas cores neutras (direção mista AO/AC). A direção do corpo da vela decide entre o tom mais escuro ou mais claro em cada par.
3. Um buffer circular armazena os índices de cor recentes junto com seus tempos de abertura. O parâmetro **SignalBar** seleciona qual barra histórica avaliar (padrão = vela anterior, ou seja, offset 1). Uma barra mais atrás é usada como contexto.
4. As entradas compradas são habilitadas quando a barra mais antiga pertencia a uma zona de momentum altista e a barra de sinal sai dessa zona. As entradas vendidas espelham a lógica com zonas baixistas. Os sinais de saída usam os mesmos filtros de momentum para fechar a direção oposta.
5. O filtro de sessão opcional (**UseTimeFilter**) mantém um registro de trading entre **StartHour:StartMinute** e **EndHour:EndMinute**. Sair da janela liquida imediatamente as posições abertas, prevenindo exposição noturna.
6. As proteções de stop-loss e take-profit são registradas através de `StartProtection`, traduzindo distâncias baseadas em pontos em passos de preço do instrumento.

## Regras de trading

- **Abrir comprado**: índice de cor da barra anterior `< 2` (AO e AC acelerando para cima) e o índice de cor da barra de sinal `> 1`. As entradas compradas são ignoradas se já estiver comprado ou se os comprados estiverem desabilitados.
- **Abrir vendido**: índice de cor da barra anterior `> 3` (AO e AC acelerando para baixo) e o índice de cor da barra de sinal `< 4`.
- **Fechar comprado**: acionado quando o índice de cor da barra mais antiga `> 3` (aceleração baixista) e as saídas compradas estão habilitadas.
- **Fechar vendido**: acionado quando o índice de cor da barra mais antiga `< 2` (aceleração altista) e as saídas vendidas estão habilitadas.
- Quando o filtro de tempo está ativo, as posições são fechadas forçosamente fora da sessão permitida mesmo sem sinais de saída baseados em cor.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Período usado para os cálculos AO/AC. | `TimeSpan.FromHours(4).TimeFrame()` |
| `Volume` | Tamanho da ordem para novas entradas. | `1m` |
| `SignalBar` | Número de velas finalizadas a pular antes de avaliar sinais (1 = vela anterior). | `1` |
| `StopLossPoints` | Distância do stop protetor em pontos do instrumento. Defina `0` para desabilitar. | `1000m` |
| `TakeProfitPoints` | Distância do objetivo de lucro em pontos do instrumento. Defina `0` para desabilitar. | `2000m` |
| `EnableLongEntries`, `EnableShortEntries` | Permitir abrir operações na direção respectiva. | `true` |
| `EnableLongExits`, `EnableShortExits` | Permitir fechar operações na direção respectiva. | `true` |
| `UseTimeFilter` | Habilitar restrições de sessão de trading. | `true` |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Limites de sessão (início inclusivo, fim exclusivo para horas idênticas). | `0/0/23/59` |

## Notas

- A estratégia sincroniza automaticamente as distâncias de stop-loss e take-profit com o passo de preço do instrumento.
- Os sinais são carimbados com o horário de fechamento da barra avaliada para que operações repetidas dentro da mesma barra sejam suprimidas.
- Nenhuma versão Python é fornecida, correspondendo à estrutura do pacote MQL fonte.
