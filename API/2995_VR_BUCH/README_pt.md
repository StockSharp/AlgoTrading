# Estratégia de Médias Móveis VR BUCH
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia de Médias Móveis VR BUCH** é um port direto do consultor especialista MetaTrader *VR---BUCH*. Ela negocia reversões de tendência usando duas médias móveis configuráveis e um filtro de preço de vela. A versão StockSharp mantém o fluxo de sinal original: a estratégia fecha as posições abertas quando um setup oposto aparece e só abre uma nova posição depois que a exposição anterior está completamente fechada.

A implementação depende das assinaturas de velas de alto nível do StockSharp, indicadores de média móvel nativos e auxiliares de ordens em tempo real. Todos os valores de indicadores são processados em velas terminadas e a estratégia evita buffers históricos manuais, exceto por um pequeno buffer circular que reproduz os parâmetros de deslocamento do MetaTrader.

## Lógica de Negociação
1. **Cálculo de indicadores**
   - Uma média móvel rápida e uma lenta são calculadas no tipo de vela selecionado.
   - Cada média móvel pode usar uma fonte de preço e método de suavização diferentes (simples, exponencial, suavizado, ponderado).
   - Deslocamentos horizontais opcionais reproduzem o parâmetro `ma_shift` do MetaTrader referenciando valores de velas passadas.
2. **Detecção de sinais**
   - Um setup de *compra* ocorre quando a MA rápida deslocada está acima da MA lenta deslocada **e** o preço de confirmação selecionado está acima da MA rápida.
   - Um setup de *venda* ocorre quando a MA rápida deslocada está abaixo da MA lenta deslocada **e** o preço de confirmação está abaixo da MA rápida.
3. **Gerenciamento de posição**
   - Se uma posição já estiver aberta, um sinal oposto primeiro fecha o saldo plano. Novas entradas são avaliadas em sinais subsequentes apenas quando a posição líquida retorna a zero.
   - Quando não há posição, a estratégia envia uma ordem de mercado com o volume configurado na direção do sinal ativo.

Nenhum nível de stop-loss ou take-profit está incluído por padrão. Os usuários podem combinar a estratégia com blocos de proteção do StockSharp (`StartProtection`) ou gerenciadores de risco externos, se necessário.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| **Fast Period** | Comprimento da média móvel rápida. |
| **Fast Shift** | Número de velas usadas para deslocar o valor da MA rápida para o passado. |
| **Fast Price** | Componente de preço da vela usado para a MA rápida (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado). |
| **Fast Method** | Método de suavização para a MA rápida (simples, exponencial, suavizado, ponderado). |
| **Slow Period** | Comprimento da média móvel lenta. |
| **Slow Shift** | Número de velas usadas para deslocar o valor da MA lenta. |
| **Slow Price** | Componente de preço da vela para a MA lenta. |
| **Slow Method** | Método de suavização para a MA lenta. |
| **Signal Price** | Preço da vela usado para confirmar a entrada (padrão é o fechamento). |
| **Candle Type** | Período ou tipo de vela personalizado usado para os cálculos. |
| **Volume** | Volume da ordem para novas negociações. |

## Notas de Uso
- Sinais são avaliados apenas em velas terminadas para evitar ruído intra-barra.
- A estratégia espera que o conector de negociação forneça dados históricos suficientes para aquecer ambas as médias móveis e seus buffers de deslocamento.
- O preço ponderado usa a fórmula \((High + Low + 2 * Close) / 4\), correspondendo à opção `PRICE_WEIGHTED` do MetaTrader.
- O nome da classe e o namespace seguem as convenções do projeto StockSharp, permitindo compilação perfeita dentro da solução `AlgoTrading`.

## Como Executar
1. Coloque a estratégia em um contêiner de estratégia StockSharp ou executor de amostras.
2. Configure o instrumento desejado, o período (`Candle Type`) e o volume da ordem.
3. Ajuste as configurações da média móvel para corresponder ao template original do MetaTrader se necessário.
4. Inicie a estratégia. Ela vai assinar velas, desenhar indicadores nos gráficos (se disponível) e colocar ordens de mercado com base na lógica descrita.

Para uso em portfólio ou com múltiplos símbolos, duplique a instância da estratégia por instrumento e atribua instrumentos dedicados.
