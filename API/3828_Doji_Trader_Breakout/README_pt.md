# Estratégia do comerciante Doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o consultor especialista MQL4 "DojiTrader" em uma amostra StockSharp C#. Ele procura velas doji recentes e negocia um rompimento da faixa doji durante as principais sessões da Europa e dos EUA.

## Lógica de negociação
- A estratégia processa apenas velas finalizadas do período selecionado (velas de 30 minutos por padrão).
- A negociação é permitida apenas entre 8h00 e 17h00 horário da plataforma.
- Embora estável, ele analisa até três velas concluídas e lembra o doji mais recente (o preço de abertura é igual ao preço de fechamento).
- Quando a vela imediatamente após o doji fecha acima da máxima do doji, um longo rompimento é armado. Se fechar abaixo da mínima doji, um pequeno rompimento será armado.
- Assim que uma vela subsequente fecha além do preço de ativação, a estratégia envia uma ordem de mercado na direção do rompimento.
- Após a entrada, o intervalo doji é mantido para controle de saída. A posição é fechada quando:
  - A vela anterior fecha dentro do intervalo (longa: fecha abaixo da mínima do doji, curta: fecha acima da máxima do doji).
  - Os extremos da vela atingem os níveis de stop loss sintético ou takeprofit que imitam as saídas de ponto fixo MQL4 originais.

## Parâmetros
- **Volume de ordens** – volume utilizado para ordens de mercado.
- **Take Profit (steps)** – distância até a meta de lucro medida em etapas de preço.
- **Stop loss (etapas)** – distância até o stop de proteção em etapas de preço.
- **Tipo de vela** – período de tempo das velas usadas para detecção de sinal.

Os cálculos de stop-loss e take-profit baseiam-se na etapa do preço do título, emulando o EA original que usava distâncias fixas de pip. Quando nenhum doji válido estiver presente nas últimas três velas, o estado de rompimento é apagado e a busca é reiniciada.
